﻿/*
 * LINQ to LDAP
 * http://linqtoldap.codeplex.com/
 * 
 * Copyright Alan Hatter (C) 2010-2014
 
 * 
 * This project is subject to licensing restrictions. Visit http://linqtoldap.codeplex.com/license for more information.
 */

using System;
using System.DirectoryServices.Protocols;
using LinqToLdap.Logging;
using LinqToLdap.Mapping;
using LinqToLdap.QueryCommands.Options;

namespace LinqToLdap.QueryCommands
{
    internal class SingleOrDefaultQueryCommand : QueryCommand
    {
        public SingleOrDefaultQueryCommand(IQueryCommandOptions options, IObjectMapping mapping)
            : base(options, mapping, true)
        {
        }

        public override object Execute(DirectoryConnection connection, SearchScope scope, int maxPageSize, bool pagingEnabled, ILinqToLdapLogger log = null, string namingContext = null)
        {
            if (Options.YieldNoResults) return Options.GetTransformer().Default();

            SetDistinguishedName(namingContext);
            SearchRequest.Scope = scope;

            if (Options.SortingOptions != null)
            {
                if (GetControl<SortRequestControl>(SearchRequest.Controls) != null)
                    throw new InvalidOperationException("Only one sort request control can be sent to the server");

                SearchRequest.Controls.Add(new SortRequestControl(Options.SortingOptions.Keys) { IsCritical = false });
            }
            if (GetControl<PageResultRequestControl>(SearchRequest.Controls) != null)
            {
                throw new InvalidOperationException("Only one page request control can be sent to the server.");
            }

            if (pagingEnabled && !Options.WithoutPaging)
            {
                SearchRequest.Controls.Add(new PageResultRequestControl(2));
            }

            if (log != null && log.TraceEnabled) log.Trace(SearchRequest.ToLogString());

            var response = connection.SendRequest(SearchRequest) as SearchResponse;

            response.AssertSuccess();
            
            if (response.Entries.Count > 1)
            {
                throw new InvalidOperationException(string.Format("SingleOrDefault returned {0} results for {1}",
                                                                  response.Entries.Count, SearchRequest.Filter));
            }

            return response.Entries.Count == 1 
                ? Options.GetTransformer().Transform(response.Entries[0]) 
                : Options.GetTransformer().Default();
        }
    }
}
