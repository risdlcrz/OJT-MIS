using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Tests
{
    /// <summary>
    /// The 5-attempt lock, end to end through Identity's SignInManager, with
    /// the same lockout options as Program.cs.
    /// </summary>
    public class AccountLockServiceTests : IAsyncLifetime
    {
        private const string Password = "Correct-Horse-1";
        private ServiceProvider _services = null!;
        private IServiceScope _scope = null!;

        private UserManager<ApplicationUser> Users => _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        private SignInManager<ApplicationUser> SignIn => _scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        private AccountLockService Locks => _scope.ServiceProvider.GetRequiredService<AccountLockService>();
        private ApplicationDbContext Db => _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        public async Task InitializeAsync()
        {
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString("N");
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = AccountLockService.MaxFailedSignInAttempts;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(365 * 100);
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddScoped<AccountLockService>();

            _services = services.BuildServiceProvider();
            _scope = _services.CreateScope();

            var roles = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in UserRoles.All) await roles.CreateAsync(new IdentityRole(role));
        }

        public async Task DisposeAsync()
        {
            _scope.Dispose();
            await _services.DisposeAsync();
        }

        private async Task<ApplicationUser> CreateUserAsync(string email, string role)
        {
            var user = new ApplicationUser
            {
                UserName = email, Email = email, FirstName = "Test", LastName = role,
                EmailConfirmed = true, IsApproved = true
            };
            Assert.True((await Users.CreateAsync(user, Password)).Succeeded);
            await Users.AddToRoleAsync(user, role);
            return user;
        }

        [Fact]
        public async Task Fifth_failed_sign_in_locks_until_HR_unlocks_and_alerts_HR()
        {
            var admin = await CreateUserAsync("hr@test.local", UserRoles.HRAdmin);
            var intern = await CreateUserAsync("intern@test.local", UserRoles.Intern);

            for (var attempt = 1; attempt < AccountLockService.MaxFailedSignInAttempts; attempt++)
            {
                var miss = await SignIn.CheckPasswordSignInAsync(intern, "wrong-password", lockoutOnFailure: true);
                Assert.False(miss.IsLockedOut);
            }

            var fifth = await SignIn.CheckPasswordSignInAsync(intern, "wrong-password", lockoutOnFailure: true);
            Assert.True(fifth.IsLockedOut);
            await Locks.LockAfterFailedSignInsAsync(intern, "203.0.113.9");

            var stored = await Users.FindByIdAsync(intern.Id);
            Assert.True(AccountLockService.IsLocked(stored!));
            Assert.Equal(DateTimeOffset.MaxValue, stored!.LockoutEnd);
            Assert.Equal(AccountLockService.SystemActor, stored.LockedByName);
            Assert.Equal(AccountLockService.FailedSignInReason, stored.LockReason);

            // Even the right password is refused while locked.
            var correct = await SignIn.CheckPasswordSignInAsync(stored, Password, lockoutOnFailure: true);
            Assert.True(correct.IsLockedOut);

            var alert = Assert.Single(Db.Notifications.Where(n => n.Type == "AccountLocked"));
            Assert.Equal(admin.Id, alert.UserId);
            Assert.Single(Db.ActivityLogs.Where(l => l.Category == ActivityLogWriter.SecurityCategory));

            // A second call for the same lock (a parallel request) does not alert twice.
            await Locks.LockAfterFailedSignInsAsync(stored, "203.0.113.9");
            Assert.Single(Db.Notifications.Where(n => n.Type == "AccountLocked"));
        }

        [Fact]
        public async Task Successful_sign_in_resets_the_counter()
        {
            var intern = await CreateUserAsync("intern@test.local", UserRoles.Intern);

            for (var attempt = 1; attempt < AccountLockService.MaxFailedSignInAttempts; attempt++)
            {
                await SignIn.CheckPasswordSignInAsync(intern, "wrong-password", lockoutOnFailure: true);
            }
            Assert.True((await SignIn.CheckPasswordSignInAsync(intern, Password, lockoutOnFailure: true)).Succeeded);
            Assert.Equal(0, (await Users.FindByIdAsync(intern.Id))!.AccessFailedCount);

            // Four more misses after the reset still do not lock.
            for (var attempt = 1; attempt < AccountLockService.MaxFailedSignInAttempts; attempt++)
            {
                Assert.False((await SignIn.CheckPasswordSignInAsync(intern, "wrong-password", lockoutOnFailure: true)).IsLockedOut);
            }
        }

        [Fact]
        public async Task HR_lock_records_who_and_why_rotates_the_stamp_and_unlock_clears_it()
        {
            var admin = await CreateUserAsync("hr@test.local", UserRoles.HRAdmin);
            var supervisor = await CreateUserAsync("sup@test.local", UserRoles.Supervisor);
            var stampBefore = supervisor.SecurityStamp;

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id),
                new Claim(ClaimTypes.Name, admin.FullName)
            }, "Test"));

            Assert.True(await Locks.LockByAdminAsync(supervisor, "  Left the agency.  ", principal));

            var locked = (await Users.FindByIdAsync(supervisor.Id))!;
            Assert.True(AccountLockService.IsLocked(locked));
            Assert.Equal(admin.Id, locked.LockedByUserId);
            Assert.Equal(admin.FullName, locked.LockedByName);
            Assert.Equal("Left the agency.", locked.LockReason);
            Assert.NotNull(locked.LockedAt);
            Assert.NotEqual(stampBefore, locked.SecurityStamp);
            Assert.Contains("Left the agency.", AccountLockService.SessionLockedMessage(locked));

            Assert.True(await Locks.UnlockAsync(locked));
            var unlocked = (await Users.FindByIdAsync(supervisor.Id))!;
            Assert.False(AccountLockService.IsLocked(unlocked));
            Assert.Null(unlocked.LockedAt);
            Assert.Null(unlocked.LockReason);
            Assert.Equal(0, unlocked.AccessFailedCount);
            Assert.True((await SignIn.CheckPasswordSignInAsync(unlocked, Password, lockoutOnFailure: true)).Succeeded);
        }
    }

    public class UploadValidatorTests
    {
        private static readonly UploadRule Avatar = new()
        {
            MaxBytes = 1024,
            MaxFiles = 1,
            AllowedContentTypes = new[] { "image/jpeg", "image/png" },
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png" }
        };

        private static string DataUrl(string type, byte[] bytes) => $"data:{type};base64,{Convert.ToBase64String(bytes)}";

        private static byte[] Jpeg(int size)
        {
            var bytes = new byte[size];
            bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;
            return bytes;
        }

        [Fact]
        public void A_real_jpeg_within_the_limit_passes() =>
            Assert.Null(UploadValidator.ValidateDataUrl(DataUrl("image/jpeg", Jpeg(512)), Avatar));

        [Fact]
        public void A_png_labelled_as_jpeg_is_rejected_by_its_bytes()
        {
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 };
            Assert.NotNull(UploadValidator.ValidateDataUrl(DataUrl("image/jpeg", png), Avatar));
        }

        [Fact]
        public void Html_or_svg_disguised_as_an_image_is_rejected()
        {
            var html = System.Text.Encoding.UTF8.GetBytes("<svg onload=alert(1)>");
            Assert.NotNull(UploadValidator.ValidateDataUrl(DataUrl("image/svg+xml", html), Avatar));
            Assert.NotNull(UploadValidator.ValidateDataUrl(DataUrl("image/png", html), Avatar));
        }

        [Fact]
        public void An_oversized_image_is_rejected() =>
            Assert.Contains("too large", UploadValidator.ValidateDataUrl(DataUrl("image/jpeg", Jpeg(4096)), Avatar));

        [Theory]
        [InlineData("https://attacker.example/pixel.png")]
        [InlineData("data:image/jpeg,not-base64")]
        [InlineData("data:image/jpeg;base64,***")]
        [InlineData("data:image/jpeg;base64,")]
        public void Anything_but_a_base64_image_data_url_is_rejected(string value) =>
            Assert.NotNull(UploadValidator.ValidateDataUrl(value, Avatar));

        [Fact]
        public void The_shared_description_lists_types_and_size() =>
            Assert.Equal("JPG or PNG, up to 1 KB", UploadValidator.Describe(Avatar));
    }

    public class SafeErrorsTests
    {
        [Fact]
        public void Json_reader_errors_are_replaced_but_field_messages_are_kept()
        {
            var errors = new Dictionary<string, string[]>
            {
                ["$.requiredHours"] = new[] { "The JSON value could not be converted to System.Int32. Path: $.requiredHours | LineNumber: 0 | BytePositionInLine: 20." },
                ["FirstName"] = new[] { "First name is required." }
            };

            var message = SafeErrors.ValidationMessage(errors);

            Assert.Contains("First name is required.", message);
            Assert.Contains(SafeErrors.InvalidFormat, message);
            Assert.DoesNotContain("System.Int32", message);
            Assert.DoesNotContain("LineNumber", message);
        }
    }
}
