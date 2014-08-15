﻿using System;
using System.Linq;
using Glittertind.Sherpa.Library.SiteHierarchy.Model;
using Microsoft.SharePoint.Client;

namespace Glittertind.Sherpa.Library.SiteHierarchy
{
    public class SiteSetupManager : ISiteSetupManager
    {
        private readonly GtWeb _configurationWeb;
        private ClientContext ClientContext { get; set; }
        private FeatureManager FeatureManager { get; set; }
        private QuicklaunchManager QuicklaunchManager { get; set; }

        public SiteSetupManager(ClientContext clientContext, GtWeb configurationWeb)
        {
            _configurationWeb = configurationWeb;
            ClientContext = clientContext;

            FeatureManager = new FeatureManager();
            QuicklaunchManager = new QuicklaunchManager();
        }
        public void SetupSites()
        {
                EnsureAndConfigureWebAndActivateFeatures(ClientContext, null, _configurationWeb);
        }

        /// <summary>
        /// Assumptions:
        /// 1. The order of webs and subwebs in the config file follows the structure of SharePoint sites
        /// 2. No config element is present without their parent web already being defined in the config file, except the root web
        /// </summary>
        private void EnsureAndConfigureWebAndActivateFeatures(ClientContext context, Web parentWeb, GtWeb configWeb)
        {
            var webToConfigure = EnsureWeb(context, parentWeb, configWeb);

            FeatureManager.ActivateFeatures(context, configWeb.SiteFeatures, configWeb.WebFeatures);
            QuicklaunchManager.CreateQuicklaunchNodes(context, webToConfigure, configWeb.Quicklaunch);

            foreach (GtWeb subWeb in configWeb.Webs)
            {
                EnsureAndConfigureWebAndActivateFeatures(context, webToConfigure, subWeb);
            }
        }

        private Web EnsureWeb(ClientContext context, Web parentWeb, GtWeb configWeb)
        {
            Web webToConfigure;
            if (parentWeb == null)
            {
                //We assume that the root web always exists
                webToConfigure = context.Site.RootWeb;
            }
            else
            {
                webToConfigure = GetSubWeb(context, parentWeb, configWeb.Url) ??
                                 parentWeb.Webs.Add(GetWebCreationInformationFromConfig(configWeb));
            }
            context.Load(webToConfigure, w => w.Url);
            context.ExecuteQuery();

            return webToConfigure;
        }

        private WebCreationInformation GetWebCreationInformationFromConfig(GtWeb configWeb)
        {
            return new WebCreationInformation
                {
                    Title = configWeb.Name,
                    Description = configWeb.Description,
                    Language = configWeb.Language,
                    Url = configWeb.Url,
                    UseSamePermissionsAsParentSite = true,
                    WebTemplate = configWeb.Template
                };
        }

        public void DeleteSites()
        {
            ClientContext.Load(ClientContext.Site.RootWeb.Webs);
            ClientContext.ExecuteQuery();

            foreach (var web in ClientContext.Site.RootWeb.Webs)
            {
                DeleteWeb(web);
            }
        }

        private void DeleteWeb(Web web)
        {
            ClientContext.Load(web.Webs);
            ClientContext.ExecuteQuery();

            foreach (Web subWeb in web.Webs)
            {
                DeleteWeb(subWeb);
            }
            web.DeleteObject();
            ClientContext.ExecuteQuery();
        }

        private Web GetSubWeb(ClientContext context, Web parentWeb, string webUrl)
        {
            context.Load(parentWeb, w => w.Url, w => w.Webs);
            context.ExecuteQuery();

            var absoluteUrlToCheck = parentWeb.Url.TrimEnd('/') + '/' + webUrl;
            // use a simple linq query to get any sub webs with the URL we want to check
            return (from w in parentWeb.Webs where w.Url == absoluteUrlToCheck select w).SingleOrDefault();
        }
    }
}
