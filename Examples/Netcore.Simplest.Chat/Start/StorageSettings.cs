using System;

namespace Netcore.Simplest.Chat.Start
{
    public class StorageSettings
    {
        public StorageType Type { get; set; }

        public MongoSettings Mongo { get; set; }

        public enum StorageType
        {
            InMemory,
            Mongo
        }

        public class MongoSettings
        {
            public static readonly string DefaultConnectionString = "mongodb://localhost";
            public static readonly string DefaultDbName = "chat";
            public MongoSettings()
            {
                ConnectionString = DefaultConnectionString;
                DbName = DefaultDbName;
            }

            public string ConnectionString { get; set; }
            public string DbName { get; set; }
            public SslSettings Ssl { get; set; }

            public class SslSettings
            {
                public SslSettings()
                {
                    VerifySslCertificate = true;
                }

                public bool VerifySslCertificate { get; set; }
            }
        }
    }
}
