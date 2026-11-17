using OJTMISApi.Services;

namespace OJTMISApi.Tests
{
    public class AttendanceQrServiceTests
    {
        private readonly AttendanceQrService _qr = new();

        [Fact]
        public void NewToken_is_opaque_and_unique()
        {
            var tokens = Enumerable.Range(0, 2000).Select(_ => _qr.NewToken()).ToList();

            Assert.Equal(tokens.Count, tokens.Distinct().Count());
            Assert.All(tokens, token =>
            {
                Assert.StartsWith("OJT2.", token);
                Assert.True(token.Length <= 64, "must fit Intern.AttendanceQrToken (64)");
                // URL-safe base64 only: nothing a scanner app could read as data.
                Assert.Matches("^OJT2\\.[A-Za-z0-9_-]{43}$", token);
            });
        }

        [Fact]
        public void Matches_only_the_issued_token()
        {
            var issued = _qr.NewToken();

            Assert.True(_qr.Matches(issued, issued));
            Assert.True(_qr.Matches($"  {issued}\n", issued)); // scanners may add whitespace
            Assert.False(_qr.Matches(_qr.NewToken(), issued));
            Assert.False(_qr.Matches(issued[..^1], issued));
            Assert.False(_qr.Matches(null, issued));
            Assert.False(_qr.Matches(issued, null));
            Assert.False(_qr.Matches(issued, ""));
        }

        [Fact]
        public void Old_OJT1_codes_are_recognised_but_never_accepted()
        {
            const string legacy = "OJT1.42.abcdefSignature";

            Assert.True(AttendanceQrService.IsLegacy(legacy));
            Assert.False(AttendanceQrService.IsLegacy(_qr.NewToken()));
            Assert.False(_qr.Matches(legacy, legacy));
        }
    }
}
