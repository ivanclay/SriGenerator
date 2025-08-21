using SriGenerator;
using SriGenerator.Models;

var path = @"D:\_DO_GIT\_____Projects\TestWebProject";
// Exemplo 1: TODOS os recursos (locais + CDNs)
using var generator = SriGenerator.SriGenerator.CreateForAllResources();

var result = await generator.ProcessProjectAsync(path);

Console.WriteLine($"Processamento concluído:");
Console.WriteLine($"- Arquivos processados: {result.TotalFilesProcessed}");
Console.WriteLine($"- Sucessos: {result.SuccessfulUpdates}");
Console.WriteLine($"- Falhas: {result.Failures}");
Console.WriteLine($"- Tempo: {result.ProcessingTime.TotalSeconds:F2}s");

Console.WriteLine("\nDetalhes:");
foreach (var item in result.Results)
{
    var status = item.Status switch
    {
        SriStatus.Added => "✅ Adicionado",
        SriStatus.Updated => "🔄 Atualizado",
        SriStatus.Skipped => "⏭️ Ignorado",
        SriStatus.Failed => "❌ Falhou",
        SriStatus.AlreadyExists => "ℹ️ Já existe",
        _ => "❓ Desconhecido"
    };

    Console.WriteLine($"  {status}: {item.ResourceUrl}");

    if (!string.IsNullOrEmpty(item.ErrorMessage))
    {
        Console.WriteLine($"    Erro: {item.ErrorMessage}");
    }
}

// Exemplo 2: Configuração para FORÇAR atualização
var customConfig = new SriConfiguration(
    HashAlgorithm: HashAlgorithmType.Sha384,
    IncludeExternalResources: true,  // ✅ PROCESSA CDNs
    CreateBackup: true,
    OverwriteExisting: true,         // ✅ SOBRESCREVE SRI existente
    ExcludePatterns: new[] { "temp", "debug" }
);

using var customGenerator = new SriGenerator.SriGenerator(customConfig);
var customResult = await customGenerator.ProcessProjectAsync(path);

Console.WriteLine($"\nProcessamento customizado: {customResult.SuccessfulUpdates} atualizações");

// Exemplo 3: Só para testar CDNs (sem sobrescrever existentes)
var cdnOnlyConfig = new SriConfiguration(
    IncludeExternalResources: true,  // Processa CDNs
    OverwriteExisting: false         // Não sobrescreve existentes
);

using var cdnGenerator = new SriGenerator.SriGenerator(cdnOnlyConfig);
var cdnResult = await cdnGenerator.ProcessProjectAsync(path);

Console.WriteLine($"\nCDNs processados: {cdnResult.SuccessfulUpdates} novos hashes");
Console.WriteLine("CDNs que foram processados:");
foreach (var item in cdnResult.Results.Where(r => !r.IsLocal && r.Status == SriStatus.Added))
{
    Console.WriteLine($"  ✅ {item.ResourceUrl}");
    Console.WriteLine($"     {item.IntegrityHash}");
}