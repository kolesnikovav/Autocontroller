using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace AutoController;
/// <summary>
/// Use for retrive params from clients request
/// </summary>
public class RequestParamName
{
    /// <summary>
    /// User difined parameter name
    /// </summary>
    public string UserDefinedValue { get; set; }=string.Empty;
    /// <summary>
    /// Type to cast request data
    /// </summary>
    public Type TypeToCast { get; set; } = typeof(string);
}
/// <summary>
/// Container for request data
/// </summary>
public class UserRequestParams
{
    /// <summary>
    /// Size of page
    /// </summary>
    public uint pageSize { get; set; } = 0;
    /// <summary>
    /// Number of page
    /// </summary>
    public uint pageNumber { get; set; } = 0;
    /// <summary>
    /// filter expression
    /// </summary>
    public string filter { get; set; } = "";
    /// <summary>
    /// sort fields
    /// </summary>
    public string sort { get; set; } = "";
    /// <summary>
    /// sort direction
    /// </summary>
    public string sortDirection { get; set; } = "";
}
/// <summary>
/// Use for retrive params from clients request
/// </summary>
public static class RequestParams
{
    /// <summary>
    /// Use for retrive params from clients request
    /// </summary>
    public static Dictionary<string, RequestParamName> Create(string page, string size, string filter, string sort, string sortdirection)
    {
        var res = new Dictionary<string, RequestParamName>
        {
            { page, new RequestParamName() { UserDefinedValue = "page", TypeToCast = typeof(uint) } },
            { size, new RequestParamName() { UserDefinedValue = "size", TypeToCast = typeof(uint) } },
            { filter, new RequestParamName() { UserDefinedValue = "filter", TypeToCast = typeof(string) } },
            { sort, new RequestParamName() { UserDefinedValue = "sort", TypeToCast = typeof(string) } },
            { sortdirection, new RequestParamName() { UserDefinedValue = "sortdirection", TypeToCast = typeof(string) } }
        };
        return res;
    }
    /// <summary>
    /// Retrive params from client request
    ///
    /// </summary>
    /// <param name="requestData">The dictinary with client request parameters</param>
    /// <param name="reqDefinitions">The typed parameter container</param>
    public static UserRequestParams RetriveQueryParam(IQueryCollection requestData, Dictionary<string, RequestParamName> reqDefinitions)
    {
        var result = new UserRequestParams();
        if (requestData.ContainsKey(reqDefinitions["page"].UserDefinedValue))
        {
            _ = uint.TryParse(requestData[reqDefinitions["page"].UserDefinedValue], out uint pageNumber);
            result.pageNumber = pageNumber;
        }
        if (requestData.ContainsKey(reqDefinitions["size"].UserDefinedValue))
        {
            _ = uint.TryParse(requestData[reqDefinitions["size"].UserDefinedValue], out uint pageSize);
            result.pageSize = pageSize;
        }
        if (requestData.ContainsKey(reqDefinitions["filter"].UserDefinedValue))
        {
            result.filter = requestData[reqDefinitions["filter"].UserDefinedValue]!;
        }
        if (requestData.ContainsKey(reqDefinitions["sort"].UserDefinedValue))
        {
            result.sort = requestData[reqDefinitions["sort"].UserDefinedValue]!;
        }
        if (requestData.ContainsKey(reqDefinitions["sortdirection"].UserDefinedValue))
        {
            result.sortDirection = requestData[reqDefinitions["sortdirection"].UserDefinedValue]!;
        }
        return result;
    }
}
