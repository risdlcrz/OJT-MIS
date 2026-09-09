<template>


    <!-- Loader -->
    <div id="preloader">
        <div id="status">
            <div class="spinner"></div>
        </div>
    </div>

    <!-- Begin page -->
    <div class="accountbg"></div>

    <div class="account-pages mt-5 pt-5">
        <div class="container">
            <div class="row justify-content-center">
                <div class="col-md-8 col-lg-5 col-xl-4">
                    <div class="card">
                        <div class="card-body">
                            
                            <div class="p-3">
                                <div class="float-text text-center">
                                    <a href="javascript: void(0);" class="logo-admin">
                                        <img src="../assets/images/logo.png" height="70" alt="logo">
                                    </a>
                                    <h4 class="font-18 mt-3 m-b-5">OJT-MIS</h4>
                                    <p class="login-note">Sign in to continue to your dashboard</p>
                                </div>
                            </div>

                            <div class="p-3">

                                <form class="form-horizontal" id="loginForm" onsubmit="return false;">
                                    <div class="loginWrapper">
                                        <div class="mb-3">
                                            <label class="form-label" for="username">Username</label>
                                            <!-- maxlength 29 (allowed length < 30). oninput/keydown/paste handlers prevent excess typing/pasting -->
                                            <input type="text" class="form-control" id="username" placeholder="Enter username" name="loginid" autocomplete="username" required
                                                   maxlength="29"
                                                   oninput="enforceMax(this,29)"
                                                   onkeydown="blockIfMax(event,this,29)"
                                                   onpaste="handlePaste(event,this,29)">
                                        </div>

                                        <div class="mb-3">
                                            <label class="form-label" for="userpassword">Password</label>
                                            <!-- maxlength 29 (allowed length < 30). oninput/keydown/paste handlers prevent excess typing/pasting -->
                                            <input type="password" class="form-control" id="userpassword" placeholder="Enter password" name="password" autocomplete="current-password" required
                                                   maxlength="29"
                                                   oninput="enforceMax(this,29)"
                                                   onkeydown="blockIfMax(event,this,29)"
                                                   onpaste="handlePaste(event,this,29)">
                                        </div>

                                        <div class="row mt-4">
                                            <div class="col-sm-6">
                                                <div class="form-check">
                                                    <input class="form-check-input" type="checkbox" value="" id="customControlInline">
                                                    <label class="form-check-label" for="customControlInline">
                                                        Remember me
                                                    </label>
                                                </div>
                                            </div>
                                            <div class="col-sm-6 text-end">
                                                <button class="btn btn-primary w-md waves-effect waves-light" type="button" onclick="validateLogin();">Log In</button>
                                            </div>
                                        </div>

                                        <div class="mb-0 row">
                                            <div class="col-12 mt-4 text-center">
                                                <a href="#" class="text-muted"><i class="mdi mdi-lock"></i>Forgot your password?</a>
                                            </div>
                                        </div>


                                        <hr>
                                    </div>
                                    
                                </form>

                            </div>


                        </div>
                    </div>
                    <div class="mt-5 text-center position-relative text-white">
                        <div class="d-none d-sm-block">
                            <span class="text-sm">System Version <span class="myAppInfo_Version text-Theme">1.0.0</span></span>
                        </div>
                        <span class="lead text-Theme text-shadow">
                            © 2026 OJT-MIS
                        </span>
                        <span class="d-none d-md-block footer-note">Developed and Maintained by Information Systems Development and Maintenance Division (ISDMD), Information Technology Office</span>

                    </div>

                </div>
            </div>
        </div>
    </div>


    <%-- Javascripts --%>
    
</template>

<script setup>
import '../assets/css/themeCSS.css'
import '../assets/css/myCSS.css'
import '../assets/css/Login.css'
</script>
