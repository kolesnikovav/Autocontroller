namespace AutoController;

/// <summary>
/// Metadata for a AutoController endpoint.
/// </summary>
public sealed class AutoControllerRouteMetadata
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="verb"></param>
    /// <param name="prefix"></param>
    /// <param name="template"></param>
    /// <param name="controller"></param>
    /// <param name="action"></param> <summary>
    /// 
    /// </summary>
    public AutoControllerRouteMetadata(string verb, string prefix, string template, string controller, string action, InteractingType interactingType)
    {
        Verb = verb;
        Prefix = prefix;
        Template = template;
        Controller = controller;
        Action = action;
        InteractingType = InteractingType;
    }

    /// <summary>
    /// HTTP method verb
    /// </summary>
    public string Verb { get; }

    /// <summary>
    /// Template
    /// </summary>
    public string Template { get; }
    /// <summary>
    /// Controller name
    /// </summary>
    /// <value></value>
    public string Controller { get; }

    /// <summary>
    /// Action name
    /// </summary>
    /// <value></value>
    public string Action { get; }
    /// <summary>
    /// Api prefix
    /// </summary>
    /// <value></value>
    public string Prefix { get; }
    /// <summary>
    /// 
    /// </summary> <summary>
    /// 
    /// </summary>
    /// <value></value>
    public InteractingType InteractingType { get; } = InteractingType.JSON;
}