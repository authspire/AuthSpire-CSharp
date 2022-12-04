using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
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
            public static string InvalidApplication = "Application could not be initialized, please check your public key, userid, app name & secret.";
            public static string ApplicationPaused = "This application is currently under construction, please try again later!";
            public static string NotInitialized = "Please initialize your application first!";
            public static string NotLoggedIn = "Please log into your application first!";
            public static string ApplicationDisabled = "Application has been disabled by the provider.";
            public static string ApplicationManipulated = "File corrupted! This program has been manipulated or cracked. This file won't work anymore.";
        }
        #endregion

        #region Structs
        private Dictionary<string, string> Variables = new Dictionary<string, string>();

        public RSAHelper rsa;
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

        public string app_name, userid, secret, currentVersion, publicKey;
        public bool initialized;
        public string endpoint = "https://api.authspire.com/v1";
        public string key, iv;

        /// <summary>
        /// Api Connection
        /// </summary>
        /// <param name="name">Name of your application found in the dashboard</param>
        /// <param name="userid">Your userid can be found in your account settings.</param>
        /// <param name="secret">Application secret found in the dashboard</param>
        /// <param name="currentVersion">Current application version.</param>
        /// <param name="publicKey">Public key found in the dashboard.</param>
        public API(string name, string userid, string secret, string currentVersion, string publicKey)
        {
            if (StringEmpty(userid) || StringEmpty(secret) || StringEmpty(currentVersion) || StringEmpty(publicKey))
            {
                Error("Invalid settings!");
                Environment.Exit(-1);
            }

            this.app_name = name;

            this.userid = userid;

            this.secret = secret;

            this.currentVersion = currentVersion;

            this.publicKey = publicKey;
            rsa = new RSAHelper(publicKey);
        }

        /// <summary>
        /// Initializes your application with AuthSpire
        /// </summary>
        public void InitApp()
        {
            key = randomKey(32);
            iv = randomKey(16);

            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("action", Convert.ToBase64String(Encoding.Default.GetBytes("app_info")));
            reqparm.Add("userid", Convert.ToBase64String(Encoding.Default.GetBytes(userid)));
            reqparm.Add("app_name", Convert.ToBase64String(Encoding.Default.GetBytes(app_name)));
            reqparm.Add("secret", aes_encrypt(secret, key, iv));
            reqparm.Add("version", aes_encrypt(currentVersion, key, iv));
            reqparm.Add("hash", aes_encrypt(SHA256CheckSum(Process.GetCurrentProcess().MainModule.FileName), key, iv));
            reqparm.Add("key", rsa.Encrypt(key));
            reqparm.Add("iv", rsa.Encrypt(iv));


            string req = Post(reqparm);

            var response = JObject.Parse(req);

            string response_status = response["status"].ToString();

            if (response_status == "success")
            {
                application.Application_Status = aes_decrypt(response["application_status"].ToString(), key, iv);
                application.Application_Hash = aes_decrypt(response["application_hash"].ToString(), key, iv);
                application.Application_Name = aes_decrypt(response["application_name"].ToString(), key, iv);
                application.Application_Version = aes_decrypt(response["application_version"].ToString(), key, iv);
                application.Update_URL = aes_decrypt(response["update_url"].ToString(), key, iv);
                application.User_Count = aes_decrypt(response["user_count"].ToString(), key, iv);

                initialized = true;
            }
            else if (response_status == "update_available")
            {
                application.Update_URL = aes_decrypt(response["update_url"].ToString(), key, iv);
                application.Application_Version = aes_decrypt(response["application_version"].ToString(), key, iv);

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

            key = randomKey(32);
            iv = randomKey(16);

            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("action", Convert.ToBase64String(Encoding.Default.GetBytes("login")));
            reqparm.Add("userid", Convert.ToBase64String(Encoding.Default.GetBytes(userid)));
            reqparm.Add("app_name", Convert.ToBase64String(Encoding.Default.GetBytes(app_name)));
            reqparm.Add("secret", aes_encrypt(secret, key, iv));
            reqparm.Add("username", aes_encrypt(username, key, iv));
            reqparm.Add("password", aes_encrypt(password, key, iv));
            reqparm.Add("hwid", aes_encrypt(GetHWID(), key, iv));
            reqparm.Add("key", rsa.Encrypt(key));
            reqparm.Add("iv", rsa.Encrypt(iv));

            string req = Post(reqparm);
            var response = JObject.Parse(req);

            string response_status = response["status"].ToString();

            if (response_status == "ok")
            {
                user.Username = aes_decrypt(response["username"].ToString(), key, iv);
                user.Email = aes_decrypt(response["email"].ToString(), key, iv);
                user.IP = aes_decrypt(response["ip"].ToString(), key, iv);
                user.Expires = aes_decrypt(response["expires"].ToString(), key, iv);
                user.HWID = aes_decrypt(response["hwid"].ToString(), key, iv);
                user.Last_Login = aes_decrypt(response["last_login"].ToString(), key, iv);
                user.Created_At = aes_decrypt(response["created_at"].ToString(), key, iv);
                user.Variable = aes_decrypt(response["variable"].ToString(), key, iv);
                user.Level = aes_decrypt(response["level"].ToString(), key, iv);
                string app_variables = aes_decrypt(response["app_variables"].ToString(), key, iv);
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
                Error(Messages.InvalidUserCredentials);
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


            key = randomKey(32);
            iv = randomKey(16);

            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("action", Convert.ToBase64String(Encoding.Default.GetBytes("register")));
            reqparm.Add("userid", Convert.ToBase64String(Encoding.Default.GetBytes(userid)));
            reqparm.Add("app_name", Convert.ToBase64String(Encoding.Default.GetBytes(app_name)));
            reqparm.Add("secret", aes_encrypt(secret, key, iv));
            reqparm.Add("username", aes_encrypt(username, key, iv));
            reqparm.Add("password", aes_encrypt(password, key, iv));
            reqparm.Add("license", aes_encrypt(license, key, iv));
            reqparm.Add("email", aes_encrypt(email, key, iv));
            reqparm.Add("hwid", aes_encrypt(GetHWID(), key, iv));
            reqparm.Add("iv", rsa.Encrypt(iv));
            reqparm.Add("key", rsa.Encrypt(key));

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

            key = randomKey(32);
            iv = randomKey(16);

            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("action", Convert.ToBase64String(Encoding.Default.GetBytes("log")));
            reqparm.Add("userid", Convert.ToBase64String(Encoding.Default.GetBytes(userid)));
            reqparm.Add("app_name", Convert.ToBase64String(Encoding.Default.GetBytes(app_name)));
            reqparm.Add("secret", aes_encrypt(secret, key, iv));
            reqparm.Add("username", aes_encrypt(username, key, iv));
            reqparm.Add("user_action", aes_encrypt(action, key, iv));
            reqparm.Add("iv", rsa.Encrypt(iv));
            reqparm.Add("key", rsa.Encrypt(key));


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

        private static string aes_decrypt(string text, string key, string IV)
        {
            byte[] textBytes = Convert.FromBase64String(text);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(IV);


            string plainText = null;
            using (AesManaged aes = new AesManaged())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform decryptor = aes.CreateDecryptor(keyBytes, ivBytes);
                using (MemoryStream ms = new MemoryStream(textBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(cs))
                        {
                            plainText = reader.ReadToEnd();
                        }
                    }
                }
            }
            return plainText;
        }

        private static string aes_encrypt(string text, string key, string IV)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(IV);

            byte[] encrypted;
            using (AesManaged aes = new AesManaged())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(keyBytes, ivBytes);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(text);
                        encrypted = ms.ToArray();
                    }
                }
            }
            return Convert.ToBase64String(encrypted);
        }

        private static string GetHWID()
        {
            return WindowsIdentity.GetCurrent().User.Value; // Not the best way to retrieve HWID, we suggest using your own way to get a more unique identifier
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

    public class RSAHelper
    {
        private readonly RSA publicKeyProv;


        public RSAHelper(string publicKey = null)
        {


            if (!string.IsNullOrEmpty(publicKey))
            {
                publicKeyProv = CreateRsaProviderFromPublicKey(publicKey);
            }
        }

        public string Encrypt(string text)
        {
            if (publicKeyProv == null)
            {
                throw new Exception("publicKeyProv is null");
            }
            return Convert.ToBase64String(publicKeyProv.Encrypt(Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.Pkcs1));
        }

        private bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        public RSA CreateRsaProviderFromPublicKey(string publicKeyString)
        {
            try
            {
                byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
                byte[] seq = new byte[15];

                var x509Key = Convert.FromBase64String(publicKeyString);


                using (MemoryStream mem = new MemoryStream(x509Key))
                {
                    using (BinaryReader binr = new BinaryReader(mem))
                    {
                        byte bt = 0;
                        ushort twobytes = 0;

                        twobytes = binr.ReadUInt16();
                        if (twobytes == 0x8130)
                            binr.ReadByte();
                        else if (twobytes == 0x8230)
                            binr.ReadInt16();
                        else
                            return null;

                        seq = binr.ReadBytes(15);
                        if (!CompareBytearrays(seq, seqOid))
                            return null;

                        twobytes = binr.ReadUInt16();
                        if (twobytes == 0x8103)
                            binr.ReadByte();
                        else if (twobytes == 0x8203)
                            binr.ReadInt16();
                        else
                            return null;

                        bt = binr.ReadByte();
                        if (bt != 0x00)
                            return null;

                        twobytes = binr.ReadUInt16();
                        if (twobytes == 0x8130)
                            binr.ReadByte();
                        else if (twobytes == 0x8230)
                            binr.ReadInt16();
                        else
                            return null;

                        twobytes = binr.ReadUInt16();
                        byte lowbyte = 0x00;
                        byte highbyte = 0x00;

                        if (twobytes == 0x8102)
                            lowbyte = binr.ReadByte();
                        else if (twobytes == 0x8202)
                        {
                            highbyte = binr.ReadByte();
                            lowbyte = binr.ReadByte();
                        }
                        else
                            return null;
                        byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                        int modsize = BitConverter.ToInt32(modint, 0);

                        int firstbyte = binr.PeekChar();
                        if (firstbyte == 0x00)
                        {
                            binr.ReadByte();
                            modsize -= 1;
                        }

                        byte[] modulus = binr.ReadBytes(modsize);

                        if (binr.ReadByte() != 0x02)
                            return null;
                        int expbytes = (int)binr.ReadByte();
                        byte[] exponent = binr.ReadBytes(expbytes);


                        var rsa = RSA.Create();
                        RSAParameters rsaKeyInfo = new RSAParameters
                        {
                            Modulus = modulus,
                            Exponent = exponent
                        };
                        rsa.ImportParameters(rsaKeyInfo);

                        return rsa;
                    }

                }
            }
            catch
            {
                MessageBox.Show("Invalid Public Key!");
                return null;
            }

        }
    }
}
