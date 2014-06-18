using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace DropboxFS
{
    [ExcludeFromCodeCoverage]
    public class ApplicationConfig
    {
        public string ApplicationKey { get; set; }
        public string ApplicationSecret { get; set; }
        public char DriveLetter { get; set; }
        public ClientAccountsCollection ClientAccounts { get; set; }

        public ApplicationConfig()
        {
            var appSettings = ConfigurationManager.AppSettings;

            try
            {
                if (appSettings.Count == 0) throw new Exception("Application configuration exception");
                ApplicationKey = appSettings["ApplicationKey"];
                ApplicationSecret = appSettings["ApplicationSecret"];
                DriveLetter = appSettings["DriveLetter"][0];
                ClientAccounts = ((ClientAccountsSection)ConfigurationManager.GetSection("clientAccountsSection")).Instances;
            }
            catch
            {
            }
        }
    }

    [ExcludeFromCodeCoverage]
    public class ClientAccountsSection : ConfigurationSection
    {
        [ConfigurationProperty("clientAccounts", IsRequired = true, IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(ClientAccount))]
        public ClientAccountsCollection Instances
        {
            get { return (ClientAccountsCollection)this["clientAccounts"]; }
            set { this["clientAccounts"] = value; }
        }
    }

    [ExcludeFromCodeCoverage]
    public class ClientAccountsCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ClientAccount();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            //set to whatever Element Property you want to use for a key
            return ((ClientAccount)element).Key;
        }
    }

    [ExcludeFromCodeCoverage]
    public class ClientAccount : ConfigurationElement
    {
        //Make sure to set IsKey=true for property exposed as the GetElementKey above
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("key", IsRequired = true)]
        public string Key
        {
            get { return (string)base["key"]; }
            set { base["key"] = value; }
        }

        [ConfigurationProperty("secret", IsRequired = true)]
        public string Secret
        {
            get { return (string)base["secret"]; }
            set { base["secret"] = value; }
        }

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }
}