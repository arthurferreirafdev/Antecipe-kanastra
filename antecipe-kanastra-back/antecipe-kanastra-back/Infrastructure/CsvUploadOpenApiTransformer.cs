using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace antecipe_kanastra.Infrastructure;

public sealed class CsvUploadOpenApiTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.RelativePath != "api/notas/analisar" || context.Description.HttpMethod != "POST")
            return Task.CompletedTask;

        // O gerador do .NET 10 descreve IFormFile em controllers como um objeto genérico.
        // Explicitar o formato binário faz o Swagger UI mostrar o seletor de arquivo.
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string> { "arquivo" },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["arquivo"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" }
                        }
                    }
                }
            }
        };
        return Task.CompletedTask;
    }
}
