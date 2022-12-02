using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AuthSpire_CSharp_Console
{
    public class API
    {
        #region ResponseMessages
        /// <summary>
        /// Messages for responses
        /// </summary>
        internal class Messages
        {
            public static string ServerOffline = "Server is currently not responding, try again later!";
            public static string RegisterInvalidLicense = "The license you entered is invalid or already taken!";
            public static string RegisterInvalidDetails = "You entered an invalid username or email!";
            public static string RegisterUsernameTaken = "This username is already taken!";
            public static string RegisterEmailTaken = "This email is already taken!";
            public static string UserExists = "A user with this username already exists!";
            public static string UserLicenseTaken = "This license is already binded to another machine!";
            public static string UserLicenseExpired = "Your license has expired!";
            public static string UserBanned = "You have been banned for violating the TOS!";
            public static string UserBlacklisted = "Your IP/HWID has been blacklisted!";
            public static string VPNBlocked = "You cannot use a vpn with our service! Please disable it.";
            public static string InvalidUser = "User doesn't exist!";
            public static string InvalidUserCredentials = "Username or password doesn't match!";
            public static string InvalidLoginInfo = "Invalid login information!";
            public static string InvalidLogInfo = "Invalid log information!";
            public static string LogLimitReached = "You can only add a maximum of 50 logs as a free user, upgrade to premium to enjoy no log limits!";
            public static string FailedToAddLog = "Failed to add log, contact the provider!";
            public static string InvalidApplication = "Application could not be initialized, please check your secret and userid.";
            public static string ApplicationPaused = "This application is currently under construction, please try again later!";
            public static string NotInitialized = "Please initialize your application first!";
            public static string NotLoggedIn = "Please log into your application first!";
            public static string ApplicationDisabled = "Application has been disabled by the provider.";
            public static string ApplicationManipulated = "File corrupted! This program has been manipulated or cracked. This file won't work anymore.";
        }
        #endregion

        #region Structs
        private Dictionary<string, string> Variables = new Dictionary<string, string>();

        public User user = new User();
        public class User
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public string IP { get; set; }
            public string Expires { get; set; }
            public string HWID { get; set; }
            public string Last_Login { get; set; }
            public string Created_At { get; set; }
            public string Variable { get; set; }
            public string Level { get; set; }
        }


        public Application application = new Application();
        public class Application
        {
            public string Application_Status { get; set; }
            public string Application_Name { get; set; }
            public string User_Count { get; set; }
            public string Application_Version { get; set; }
            public string Update_URL { get; set; }
            public string Application_Hash { get; set; }
        }
        #endregion

        public string app_name, userid, secret, currentVersion;
        public bool initialized;
        public string endpoint = "https://api.authspire.com/v1";
        public string iv;

        /// <summary>
        /// Api Connection
        /// </summary>
        /// <param name="name">Name of your application found in the dashboard</param>
        /// <param name="userid">Your userid can be found in your account settings.</param>
        /// <param name="secret">Application secret found in the dashboard</param>
        /// <param name="currentVersion">Current application version.</param>
        public API(string name, string userid, string secret, string currentVersion)
        {
            if (StringEmpty(userid) || StringEmpty(secret) || StringEmpty(currentVersion))
            {
                Error("Invalid settings!");
                Environment.Exit(-1);
            }

            this.app_name = name;

            this.userid = userid;

            this.secret = secret;

            this.currentVersion = currentVersion;
        }

        /// <summary>
        /// Initializes your application with AuthSpire
        /// </summary>
        public void InitApp()
        {
            iv = randomKey(16);

            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("action", Convert.ToBase64String(Encoding.Default.GetBytes("app_info")));
            reqparm.Add("userid", Convert.ToBase64String(Encoding.Default.GetBytes(userid)));
            reqparm.Add("app_name", Convert.ToBase64String(Encoding.Default.GetBytes(app_name)));
            reqparm.Add("secret", aes_encrypt(secret, iv));
            reqparm.Add("version", aes_encrypt(currentVersion, iv));
            reqparm.Add("hash", aes_encrypt(SHA256CheckSum(Process.GetCurrentProcess().MainModule.FileName), iv));
            reqparm.Add("iv", iv);


            string req = Post(reqparm);

            var response = JObject.Parse(req);

            string response_status = response["status"].ToString();

            if (response_status == "success")
            {
                application.Application_Status = aes_decrypt(response["application_status"].ToString(), iv);
                application.Application_Hash = aes_decrypt(response["application_hash"].ToString(), iv);
                application.Application_Name = aes_decrypt(response["application_name"].ToString(), iv);
                application.Application_Version = aes_decrypt(response["application_version"].ToString(), iv);
                application.Update_URL = aes_decrypt(response["update_url"].ToString(), iv);
                application.User_Count = aes_decrypt(response["user_count"].ToString(), iv);

                initialized = true;
            }
            else if (response_status == "update_available")
            {
                application.Update_URL = aes_decrypt(response["update_url"].ToString(), iv);
                application.Application_Version = aes_decrypt(response["application_version"].ToString(), iv);

                UpdateApplication(application.Update_URL, application.Application_Version);
                return;
            }
            else if (response_status == "invalid_hash")
            {
                Error(Messages.ApplicationManipulated);
                Environment.Exit(-1);
            }
            else if (response_status == "invalid_app")
            {
                Error(Messages.InvalidApplication);
                Environment.Exit(-1);
            }
            else if (response_status == "paused")
            {
                Error(Messages.ApplicationPaused);
                Environment.Exit(-1);
            }
            else if (response_status == "locked")
            {
                Error(Messages.ApplicationDisabled);
                Environment.Exit(-1);
            }
        }


        /// <summary>
        /// Authenticates the user with their username and password
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// </summary>
        public bool Login(string username, string password)
        {
            if (!initialized)
            {
                Error(Messages.NotInitialized);
                return false;
            }

            if (StringEmpty(username) || StringEmpty(password))
            {
                Error(Messages.InvalidLoginInfo);
                return false;
            }

            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("action", Convert.ToBase64String(Encoding.Default.GetBytes("login")));
            reqparm.Add("userid", Convert.ToBase64String(Encoding.Default.GetBytes(userid)));
            reqparm.Add("app_name", Convert.ToBase64String(Encoding.Default.GetBytes(app_name)));
            reqparm.Add("username", aes_encrypt(username, iv));
            reqparm.Add("password", aes_encrypt(password, iv));
            reqparm.Add("hwid", aes_encrypt(GetHWID(), iv));
            reqparm.Add("iv", iv);

            string req = Post(reqparm);
            var response = JObject.Parse(req);

            string response_status = response["status"].ToString();

            if (response_status == "ok")
            {
                user.Username = response["username"].ToString();
                user.Email = response["email"].ToString();
                user.IP = response["ip"].ToString();
                user.Expires = response["expires"].ToString();
                user.HWID = response["hwid"].ToString();
                user.Last_Login = response["last_login"].ToString();
                user.Created_At = response["created_at"].ToString();
                user.Variable = response["variable"].ToString();
                user.Level = response["level"].ToString();
                string app_variables = response["app_variables"].ToString();
                foreach (string app_variable in app_variables.Split(';'))
                {
                    string[] app_variable_split = app_variable.Split(':');
                    try
                    {
                        Variables.Add(app_variable_split[0], app_variable_split[1]);
                    }
                    catch { }
                }

                return true;
            }
            else if (response_status == "invalid_user")
            {
                return false;
            }
            else if (response_status == "invalid_details")
            {
                Error(Messages.InvalidUserCredentials);
                return false;
            }
            else if (response_status == "license_expired")
            {
                Error(Messages.UserLicenseExpired);
                Environment.Exit(0);
                return false;
            }
            else if (response_status == "invalid_hwid")
            {
                Error(Messages.UserLicenseTaken);
                Environment.Exit(0);
                return false;
            }
            else if (response_status == "banned")
            {
                Error(Messages.UserBanned);
                Environment.Exit(0);
                return false;
            }
            else if (response_status == "blacklisted")
            {
                Error(Messages.UserBlacklisted);
                Environment.Exit(0);
                return false;
            }
            else if (response_status == "vpn_blocked")
            {
                Error(Messages.VPNBlocked);
                Environment.Exit(0);
                return false;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Register a user to your application with their username, password, license and email
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="license">License</param>
        /// <param name="email">Email (can be empty)</param>
        public bool Register(string username, string password, string license, string email)
        {
            if (!initialized)
            {
                Error(Messages.NotInitialized);
                return false;
            }

            if (StringEmpty(username) || StringEmpty(password) || StringEmpty(license))
            {
                Error(Messages.InvalidLoginInfo);
                return false;
            }


            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("action", Convert.ToBase64String(Encoding.Default.GetBytes("register")));
            reqparm.Add("userid", Convert.ToBase64String(Encoding.Default.GetBytes(userid)));
            reqparm.Add("app_name", Convert.ToBase64String(Encoding.Default.GetBytes(app_name)));
            reqparm.Add("username", aes_encrypt(username, iv));
            reqparm.Add("password", aes_encrypt(password, iv));
            reqparm.Add("license", aes_encrypt(license, iv));
            reqparm.Add("email", aes_encrypt(email, iv));
            reqparm.Add("hwid", aes_encrypt(GetHWID(), iv));
            reqparm.Add("iv", iv);

            string req = Post(reqparm);
            var response = JObject.Parse(req);

            string response_status = response["status"].ToString();

            if (response_status == "user_added")
            {
                return true;
            }
            else if (response_status == "invalid_details")
            {
                Error(Messages.RegisterInvalidDetails);
                return false;
            }
            else if (response_status == "email_taken")
            {
                Error(Messages.RegisterEmailTaken);
                return false;
            }
            else if (response_status == "invalid_license")
            {
                Error(Messages.RegisterInvalidLicense);
                return false;
            }
            else if (response_status == "user_already_exists")
            {
                Error(Messages.UserExists);
                return false;
            }
            else if (response_status == "blacklisted")
            {
                Error(Messages.UserBlacklisted);
                return false;
            }
            else if (response_status == "vpn_blocked")
            {
                Error(Messages.VPNBlocked);
                return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Registers user by only using his license (has to be run twice, first time for registering, second time for logging in and retrieving user data)
        /// </summary>
        /// <param name="license">License Key</param>
        public bool LicenseOnly(string license)
        {
            if (!initialized)
            {
                Error(Messages.NotInitialized);
                return false;
            }

            if (StringEmpty(license))
            {
                Error(Messages.InvalidLoginInfo);
                return false;
            }

            if (Login(license, license))
            {
                return true;
            }
            else
            {
                if (Register(license, license, license, license))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        /// <summary>
        /// Adds a log containing a username and action to your panel
        /// </summary>
        /// <param name="action">Action that happens</param>
        /// <param name="username">Username of the user or custom name</param>
        public void AddLog(string username, string action)
        {
            if (!initialized)
            {
                Error(Messages.NotInitialized);
                Environment.Exit(-1);
            }

            if (StringEmpty(username) || StringEmpty(action))
            {
                Error(Messages.InvalidLogInfo);
                Environment.Exit(-1);
            }

            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("action", Convert.ToBase64String(Encoding.Default.GetBytes("log")));
            reqparm.Add("userid", Convert.ToBase64String(Encoding.Default.GetBytes(userid)));
            reqparm.Add("app_name", Convert.ToBase64String(Encoding.Default.GetBytes(app_name)));
            reqparm.Add("username", aes_encrypt(username, iv));
            reqparm.Add("user_action", aes_encrypt(action, iv));
            reqparm.Add("iv", iv);


            string req = Post(reqparm);
            var response = JObject.Parse(req);

            string response_status = response["status"].ToString();

            if (response_status == "log_added")
            {
                return;
            }
            else if (response_status == "failed")
            {
                Error(Messages.FailedToAddLog);
                Environment.Exit(0);
            }
            else if (response_status == "invalid_log_info")
            {
                Error(Messages.InvalidLogInfo);
                Environment.Exit(0);
            }
            else if (response_status == "log_limit_reached")
            {
                Error(Messages.LogLimitReached);
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Gets the variable using a secret key
        /// </summary>
        /// <param name="secret">Secret of your variable found in your application dashboard -> variables</param>
        public string GetVariable(string secret)
        {
            if (!initialized)
            {
                Error(Messages.NotInitialized);
                Environment.Exit(-1);
            }

            if (user.Username == null || user.HWID == null)
            {
                Error(Messages.NotLoggedIn);
                Environment.Exit(-1);
            }

            try
            {
                return Variables[secret];
            }
            catch
            {
                return "N/A";
            }
        }

        /// <summary>
        /// Opens update url if update is available
        /// </summary>
        private void UpdateApplication(string updateURL, string version)
        {
            var result = MessageBox.Show("Update " + version + " available! Install it now? ", application.Application_Name, MessageBoxButtons.YesNo, MessageBoxIcon.Error);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(application.Update_URL);
                }
                catch { }
                Environment.Exit(0);
            }
            else
            {
                Environment.Exit(0);
            }


        }
        /// <summary>
        /// Post request to the server
        /// </summary>
        private string Post(NameValueCollection parameters)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    var result = wc.UploadValues(endpoint, parameters);
                    return Encoding.Default.GetString(result);
                }
                catch (Exception exc)
                {
                    Error(Messages.ServerOffline + "\n" + exc.Message);
                    Environment.Exit(-1);
                    return "";
                }
            }
        }

        private void Error(string message)
        {
            MessageBox.Show(message, app_name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private readonly Random random = new Random();
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private string randomKey(int length)
        {
            var sb = new StringBuilder();
            for (int i = 1; i <= length; i++)
            {
                var randomCharacterPosition = random.Next(0, alphabet.Length);
                sb.Append(alphabet[randomCharacterPosition]);
            }
            return sb.ToString();
        }

        private string aes_decrypt(string input, string IV)
        {
            try
            {
                string decryptedData = null;


                byte[] encryptedData = Convert.FromBase64String(input);
                byte[] bKey = Encoding.Default.GetBytes(secret);
                byte[] bIV = Encoding.Default.GetBytes(IV);


                using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Key = bKey;
                    aes.IV = bIV;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (MemoryStream Memory = new MemoryStream(encryptedData))
                    {
                        using (CryptoStream Decryptor = new CryptoStream(Memory, decryptor, CryptoStreamMode.Read))
                        {
                            using (MemoryStream tempMemory = new MemoryStream())
                            {
                                byte[] Buffer = new byte[1024];
                                Int32 readBytes = 0;
                                while ((readBytes = Decryptor.Read(Buffer, 0, Buffer.Length)) > 0)
                                {
                                    tempMemory.Write(Buffer, 0, readBytes);
                                }
                                decryptedData = Encoding.UTF8.GetString(tempMemory.ToArray());
                            }
                        }
                    }
                }
                return decryptedData;
            }
            catch { return null; }

        }

        private string aes_encrypt(string input, string IV)
        {
            try
            {
                byte[] bKey = Encoding.Default.GetBytes(secret);
                byte[] bIV = Encoding.Default.GetBytes(IV);
                byte[] bData = Encoding.Default.GetBytes(input);
                byte[] encrypted;

                using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    //assign AES key and mode, pass key(32) and IV(16)
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Key = bKey;
                    aes.IV = bIV;
                    aes.Padding = PaddingMode.PKCS7;

                    //Create encryptor
                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    //Create encrypted stream
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoEncryptStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoEncryptStream.Write(bData, 0, bData.Length);
                            cryptoEncryptStream.FlushFinalBlock();
                        }

                        encrypted = memoryStream.ToArray();

                    }
                }

                return Convert.ToBase64String(encrypted);
            }
            catch { return null; }
        }


        private static string GetHWID()
        {
            return WindowsIdentity.GetCurrent().User.Value;
        }

        private static string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = SHA256Managed.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                    return BitConverter.ToString(SHA256.ComputeHash(fileStream)).Replace("-", "");
            }
        }

        private static bool StringEmpty(string check)
        {
            return string.IsNullOrEmpty(check);
        }
    }
}
