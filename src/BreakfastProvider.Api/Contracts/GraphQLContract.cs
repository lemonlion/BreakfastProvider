using HotChocolate.Execution;

namespace BreakfastProvider.Api.Contracts;

/// <summary>The GraphQL contract as JSON: the standard introspection response, produced inside the service.</summary>
public static class GraphQLContract
{
    /// <summary>
    /// graphql-js's getIntrospectionQuery with descriptions, specifiedByUrl, directiveIsRepeatable,
    /// schemaDescription and inputValueDeprecation on.
    /// </summary>
    public const string IntrospectionQuery = """
        query IntrospectionQuery {
          __schema {
            description
            queryType { name }
            mutationType { name }
            subscriptionType { name }
            types { ...FullType }
            directives { name description isRepeatable locations args(includeDeprecated: true) { ...InputValue } }
          }
        }
        fragment FullType on __Type {
          kind name description specifiedByURL
          fields(includeDeprecated: true) {
            name description args(includeDeprecated: true) { ...InputValue } type { ...TypeRef } isDeprecated deprecationReason
          }
          inputFields(includeDeprecated: true) { ...InputValue }
          interfaces { ...TypeRef }
          enumValues(includeDeprecated: true) { name description isDeprecated deprecationReason }
          possibleTypes { ...TypeRef }
        }
        fragment InputValue on __InputValue {
          name description type { ...TypeRef } defaultValue isDeprecated deprecationReason
        }
        fragment TypeRef on __Type {
          kind name
          ofType { kind name ofType { kind name ofType { kind name ofType { kind name
            ofType { kind name ofType { kind name ofType { kind name ofType { kind name } } } } } } } }
        }
        """;

    /// <summary>
    /// Runs the introspection query inside the service. HotChocolate refuses client introspection outside
    /// Development, so the published document is produced here, with introspection allowed for this request only.
    /// </summary>
    public static async Task<string> IntrospectAsync(IRequestExecutorResolver executors, CancellationToken cancellationToken)
    {
        var executor = await executors.GetRequestExecutorAsync(cancellationToken: cancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(IntrospectionQuery)
            .AllowIntrospection()   // this request only: client introspection keeps HotChocolate's default
            .Build();
        var result = await executor.ExecuteAsync(request, cancellationToken);
        return result.ToJson();
    }
}
