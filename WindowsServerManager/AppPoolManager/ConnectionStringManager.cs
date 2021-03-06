﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using AppPoolManager.Tools;
using Microsoft.Web.Administration;

namespace AppPoolManager
{
    public class ConnectionStringManager: IDisposable
    {
        private readonly SitesManager _sitesManager;
        private readonly string _redisConnectionStringKey;
        private readonly string _appConfigFineName;

        public ConnectionStringManager(string redisConnectionStringKey, string appConfigFineName)
        {
            _redisConnectionStringKey = redisConnectionStringKey;
            _appConfigFineName = appConfigFineName;
            _sitesManager = new SitesManager();
        }

        public ConnectionStringsSection GetSiteConnectionStrings(string siteName, bool isSite)
        {
            Application rootApplication = null;
            if (isSite)
            {
                var site = _sitesManager.GetSiteByName(siteName);
                var siteApplications = site.Applications;
                rootApplication = siteApplications.SingleOrDefault(x => x.Path == "/");
            }
            else
            {
                rootApplication = _sitesManager.GetApplicationByPath(siteName);
            }

            var connectionStringFileUrl = "";
            var dictionary = rootApplication.VirtualDirectories.FirstOrDefault();
            connectionStringFileUrl += $@"{dictionary.PhysicalPath}\{_appConfigFineName}";

            var map = new ExeConfigurationFileMap { ExeConfigFilename = connectionStringFileUrl };
            var configFile = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);

            return configFile.ConnectionStrings;
        }

        public string GetRedisDb(string siteName, bool isSite)
        {
            var connectionStrings = GetSiteConnectionStrings(siteName, isSite);
            var redisConnectionString = connectionStrings.ConnectionStrings[_redisConnectionStringKey];

            if (redisConnectionString != null)
            {
                return ConnectionStringsTool.GetSectionFromString(
                    redisConnectionString.ConnectionString, "db");
            }

            return null;
        }

        public string GetMssqlDb(string siteName, bool isSite)
        {
            var connectionStrings = GetSiteConnectionStrings(siteName, isSite);
            var redisConnectionString = connectionStrings.ConnectionStrings["db"];

            if (redisConnectionString != null)
            {
                return ConnectionStringsTool.GetSectionFromString(
                    redisConnectionString.ConnectionString, "Initial Catalog");
            }

            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool _disposed = false;

        public virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _sitesManager.Dispose();
            }
            _disposed = true;
        }
    }
}