﻿using System.Security;
using Glittertind.Sherpa.Library.Taxonomy.Model;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;

namespace Glittertind.Sherpa.Library.Taxonomy
{
    public class TaxonomyManager : ITaxonomyManager
    {
        private readonly SecureString _password;
        private readonly string _userName;
        private readonly string _urlToWeb;
        private readonly int _lcid;
        public IPersistanceProvider<TermItemBase> Provider { get; set; }

        public TaxonomyManager(string userName, string password, string urlToWeb, int lcid, IPersistanceProvider<TermItemBase> provider)
        {
            Provider = provider;
            _urlToWeb = urlToWeb;
            _userName = userName;
            _lcid = lcid;
            _password = new SecureString();
            foreach (char c in password) _password.AppendChar(c);
        }

        public void WriteTaxonomyToTermStore()
        {
            var group = (TermSetGroup) Provider.Load();
            using (var context = new ClientContext(_urlToWeb))
            {
                // user must be termstore admin
                context.Credentials = new SharePointOnlineCredentials(_userName, _password);
                TaxonomySession taxonomySession = TaxonomySession.GetTaxonomySession(context);
                taxonomySession.UpdateCache();
                context.Load(taxonomySession);
                context.ExecuteQuery();

                TermStore termStore = taxonomySession.GetDefaultSiteCollectionTermStore();
                context.Load(termStore);
                context.ExecuteQuery();

                context.Load(termStore.Groups);
                context.ExecuteQuery();

                var spgroup = termStore.CreateGroup(group.Title, group.Id);
                context.Load(spgroup);
                context.ExecuteQuery();

                foreach (var termSet in group.TermSets)
                {
                    context.Load(spgroup.TermSets);
                    context.ExecuteQuery();
                    var spTermSet = spgroup.CreateTermSet(termSet.Title, termSet.Id, _lcid);
                    context.Load(spTermSet);
                    context.ExecuteQuery();

                    foreach (var term in termSet.Terms)
                    {
                        context.Load(spTermSet.Terms);
                        context.ExecuteQuery();

                        var spterm = spTermSet.CreateTerm(term.Title, _lcid, term.Id);
                        context.Load(spterm);
                        context.ExecuteQuery();
                    }

                }


            }

        }


    }
}