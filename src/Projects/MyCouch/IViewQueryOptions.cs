﻿using System.Collections.Generic;

namespace MyCouch
{
    public interface IViewQueryOptions
    {
        /// <summary>
        /// Allow the results from a stale view to be used.
        /// </summary>
        string Stale { get; set; }

        /// <summary>
        /// Include the full content of the documents in the return.
        /// </summary>
        bool IncludeDocs { get; set; }
        
        /// <summary>
        /// Return the documents in descending by key order.
        /// </summary>
        bool Descending { get; set; }
        
        /// <summary>
        /// Return only documents that match the specified key.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Returns only documents that matches any of the specified keys.
        /// </summary>
        string[] Keys { get; set; }

        /// <summary>
        /// Return records starting with the specified key.
        /// </summary>
        string StartKey { get; set; }
        
        /// <summary>
        /// Return records starting with the specified document ID.
        /// </summary>
        string StartKeyDocId { get; set; }
        
        /// <summary>
        /// Stop returning records when the specified key is reached.
        /// </summary>
        string EndKey { get; set; }
        
        /// <summary>
        /// Stop returning records when the specified document ID is reached.
        /// </summary>
        string EndKeyDocId { get; set; }

        /// <summary>
        /// Specifies whether the specified end key should be included in the result.
        /// </summary>
        bool InclusiveEnd { get; set; }
        
        /// <summary>
        /// Skip this number of records before starting to return the results.
        /// </summary>
        int Skip { get; set; }
        
        /// <summary>
        /// Limit the number of the returned documents to the specified number.
        /// </summary>
        int Limit { get; set; }
        
        /// <summary>
        /// Use the reduction function.
        /// </summary>
        bool Reduce { get; set; }

        /// <summary>
        /// Include the update sequence in the generated results.
        /// </summary>
        bool UpdateSeq { get; set; }

        /// <summary>
        /// The group option controls whether the reduce function reduces to a set of distinct keys or to a single result row.
        /// </summary>
        bool Group { get; set; }

        /// <summary>
        /// Specify the group level to be used.
        /// </summary>
        int GroupLevel { get; set; }

        IEnumerable<KeyValuePair<string, string>> ToKeyValues();
    }
}