using SriGenerator.Models;
using SriGenerator.Services;

namespace SriGenerator;

/// <summary>
/// Facade principal para geração de SRI (Subresource Integrity) em projetos web
/// </summary>
public class SriGenerator : IDisposable
{
    private readonly SriGeneratorService _service;
    private bool _disposed = false;

    public SriGenerator(SriConfiguration? configuration = null)
    {
        _service = new SriGeneratorService(configuration);
    }

    /// <summary>
    /// Processa um projeto inteiro, gerando SRI para todos os recursos encontrados
    /// </summary>
    /// <param name="projectPath">Caminho para o diretório do projeto</param>
    /// <returns>Resultado do processamento com estatísticas e detalhes</returns>
    public async Task<ProcessingResult> ProcessProjectAsync(string projectPath)
    {
        return await _service.ProcessProjectAsync(projectPath);
    }

    /// <summary>
    /// Cria uma instância com configuração padrão
    /// </summary>
    public static SriGenerator CreateDefault()
    {
        return new SriGenerator();
    }

    /// <summary>
    /// Cria uma instância configurada para processar apenas recursos locais
    /// </summary>
    public static SriGenerator CreateForLocalResourcesOnly()
    {
        var config = new SriConfiguration(
            HashAlgorithm: HashAlgorithmType.Sha384,
            IncludeExternalResources: false,
            CreateBackup: true
        );
        return new SriGenerator(config);
    }

    /// <summary>
    /// Cria uma instância configurada para processar todos os recursos (locais e externos)
    /// </summary>
    public static SriGenerator CreateForAllResources()
    {
        var config = new SriConfiguration(
            HashAlgorithm: HashAlgorithmType.Sha384,
            IncludeExternalResources: true,
            CreateBackup: true
        );
        return new SriGenerator(config);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _service?.Dispose();
            }
            _disposed = true;
        }
    }
}