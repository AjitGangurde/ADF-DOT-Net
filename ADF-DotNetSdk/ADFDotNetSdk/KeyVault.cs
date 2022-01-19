using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ADFDotNetSDK
{
    /// <summary>
    /// Key Value helper class to read the secret from the key Vault.
    /// </summary>
    public class KeyVault
    {
        private static KeyVault _defaultInstance = null;
        public Uri VaultUrl { get; private set; }
        public string ClientAppID { get; private set; }
        public string AuthCertThumbprint { get; private set; }
        private CancellationToken cancelToken;
        public static KeyVault DefaultInstance
        {
            get
            {
                if (_defaultInstance == null)
                {
                    PopulateDefaultInstance();
                }
                return _defaultInstance;
            }
        }

        /// <summary>
        /// sets  the default instance
        /// </summary>
        private static void PopulateDefaultInstance()
        {
            string vaultUrl = Config.VaultUrl;
            string appId = Config.ProdClientId;
            string certThumbprint = Config.CertThumbprint;
            _defaultInstance = new KeyVault(new Uri(vaultUrl), appId, certThumbprint);

        }

        /// <summary>
        /// Constructor for default instance
        /// </summary>
        /// <param name="vaultUrl">key vault url</param>
        /// <param name="clientAppID"> application id </param>
        /// <param name="authCertThumbprint">Certificate thumb-print</param>
        /// <param name="ct">optional parameter:Cancellation token</param>
        public KeyVault(Uri vaultUrl, string clientAppID, string authCertThumbprint, CancellationToken ct = default(CancellationToken))
        {
            VaultUrl = vaultUrl;
            ClientAppID = clientAppID;
            AuthCertThumbprint = authCertThumbprint;
            cancelToken = ct;
        }

        /// <summary>
        /// Get the Key vault Client
        /// </summary>
        /// <param name="aClientId">application id </param>
        /// <param name="aCerificateThumbprint">Certificate thumb-print</param>
        /// <returns>key vault client</returns>
        [ExcludeFromCodeCoverage]
        private KeyVaultClient GetVaultClient(string aClientId, string aCerificateThumbprint)
        {
            var certificate = FindCertificateByThumbprint(aCerificateThumbprint);
            var assertionCert = new ClientAssertionCertificate(aClientId, certificate);
            Task<string> callback(string authority, string resource, string scope) => GetAccessToken(authority, resource, scope, assertionCert);

            try
            {
                var keyVaultClient = new KeyVaultClient(callback);

                return keyVaultClient;
            }
            catch (Exception e)
            {
                //StackFrame frame = new StackFrame();
                //var test = frame.GetMethod().DeclaringType.Assembly.FullName;
                //AppLoging.LogException(e, Assembly.GetCallingAssembly().GetName().Name);
                throw;
            }
        }

        /// <summary>
        /// Gets the access token
        /// </summary>
        /// <param name="authority"> Authority </param>
        /// <param name="resource"> Resource </param>
        /// <param name="scope"> scope </param>
        /// <param name="assertionCert">Certificate</param>
        /// <returns>token</returns>
        private async Task<string> GetAccessToken(string authority, string resource, string scope, ClientAssertionCertificate assertionCert)
        {
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var result = await context.AcquireTokenAsync(resource, assertionCert).ConfigureAwait(false);
            return result.AccessToken;
        }

        /// <summary>
        /// Gets the secret value for specified key
        /// </summary>
        /// <param name="aSecretName">Name of key</param>
        /// <returns>secret value</returns>
        public string GetSecretValue(string aSecretName)
        {
            string secretValue = GetSecret(aSecretName);
            return secretValue;
        }

        /// <summary>
        /// Gets the secret object
        /// </summary>
        /// <param name="aSecretName">Name of keys</param>
        /// <returns>secret object
        /// </returns>
        private string GetSecret(string aSecretName)
        {
            string SecretValue = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(aSecretName))
                {
                    ///Secret can be read via multiple way. Using certificate of using azure  service token provider. If KeyVault Using Certificate is true then certificate code will be used to read the secret 
                    if (Config.KeyvaultUsingCertificate)
                    {
                        ///Create Key vault client using the certificate thumb print and client App Id
                        KeyVaultClient kvc = GetVaultClient(ClientAppID, AuthCertThumbprint);

                        if (kvc != null)
                        {
                            var secret = kvc.GetSecretAsync(VaultUrl.AbsoluteUri, aSecretName).Result;

                            if (secret != null && !string.IsNullOrEmpty(secret.Value))
                            {
                                SecretValue = secret.Value;
                            }
                        }
                    }
                    else
                    {

                        AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                        //CreateKyvault client with managed identities.
                        var keyVaultClient = new KeyVaultClient(
                            new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                        ///Read the secret from the key vault.
                        var secret = keyVaultClient.GetSecretAsync(Config.VaultUrl, aSecretName).Result;

                        ///check secret is not null or empty before returning it.
                        if (secret != null && !string.IsNullOrEmpty(secret.Value))
                        {
                            SecretValue = secret.Value;
                        }

                    }
                }
            }
            catch (Exception e)
            {
                //AppLoging.LogException(e, Assembly.GetCallingAssembly().GetName().Name);
                throw;
            }

            return SecretValue;
        }

        /// <summary>
        /// Finds the certificate by thumb print
        /// </summary>
        /// <param name="certificateThumbprint">Thumb print of certificate</param>
        /// <returns></returns>
        public static X509Certificate2 FindCertificateByThumbprint(string certificateThumbprint)
        {

            foreach (StoreLocation storeLocation in (StoreLocation[])
                Enum.GetValues(typeof(StoreLocation)))
            {
                foreach (StoreName storeName in (StoreName[])
                    Enum.GetValues(typeof(StoreName)))
                {
                    X509Store store = new X509Store(storeName, storeLocation);

                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection col = store.Certificates.Find(X509FindType.FindByThumbprint, certificateThumbprint, false);

                    if (col != null && col.Count != 0)
                    {
                        foreach (X509Certificate2 cert in col)
                        {
                            if (cert.HasPrivateKey)
                            {
                                store.Close();
                                return cert;
                            }
                        }
                    }
                }
            }

            throw new Exception(string.Format("Could not find certificate with thumb print {0} in any certificate store.", certificateThumbprint));
        }
    }
}
