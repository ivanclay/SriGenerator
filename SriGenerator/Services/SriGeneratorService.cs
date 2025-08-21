using System.Security.Cryptography;
using System.Text;
using HtmlAgilityPack;
using SriGenerator.Models;

namespace SriGenerator.Services;

public class SriGeneratorService
{
    private readonly HttpClient _httpClient;
    private readonly SriConfiguration _configuration;

    public SriGeneratorService(SriConfiguration? configuration = null)
    {
        _configuration = configuration ?? new SriConfiguration();
        _httpClient = new HttpClient();
    }

    public async Task<ProcessingResult> ProcessProjectAsync(string projectPath)
    {
        var startTime = DateTime.UtcNow;
        var results = new List<SriResult>();

        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException($"Projeto não encontrado: {projectPath}");
        }

        var htmlFiles = GetHtmlFiles(projectPath);

        foreach (var file in htmlFiles)
        {
            try
            {
                var fileResults = await ProcessFileAsync(file, projectPath);
                results.AddRange(fileResults);
            }
            catch (Exception ex)
            {
                results.Add(new SriResult(
                    file,
                    "",
                    "",
                    ResourceType.Unknown,
                    false,
                    SriStatus.Failed,
                    ex.Message
                ));
            }
        }

        var processingTime = DateTime.UtcNow - startTime;
        var successCount = results.Count(r => r.Status == SriStatus.Added || r.Status == SriStatus.Updated);
        var failureCount = results.Count(r => r.Status == SriStatus.Failed);

        return new ProcessingResult(
            htmlFiles.Count(),
            successCount,
            failureCount,
            processingTime,
            results.AsReadOnly()
        );
    }

    private async Task<List<SriResult>> ProcessFileAsync(string filePath, string projectRoot)
    {
        var results = new List<SriResult>();

        if (_configuration.CreateBackup)
        {
            CreateBackup(filePath);
        }

        var content = await File.ReadAllTextAsync(filePath);
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var modified = false;

        // Processar scripts JavaScript
        var scriptNodes = doc.DocumentNode.SelectNodes("//script[@src]");
        if (scriptNodes != null)
        {
            foreach (var node in scriptNodes)
            {
                var result = await ProcessScriptNodeAsync(node, filePath, projectRoot);
                results.Add(result);
                if (result.Status == SriStatus.Added || result.Status == SriStatus.Updated)
                {
                    modified = true;
                }
            }
        }

        // Processar CSS
        var linkNodes = doc.DocumentNode.SelectNodes("//link[@rel='stylesheet' and @href]");
        if (linkNodes != null)
        {
            foreach (var node in linkNodes)
            {
                var result = await ProcessLinkNodeAsync(node, filePath, projectRoot);
                results.Add(result);
                if (result.Status == SriStatus.Added || result.Status == SriStatus.Updated)
                {
                    modified = true;
                }
            }
        }

        if (modified)
        {
            await File.WriteAllTextAsync(filePath, doc.DocumentNode.OuterHtml);
        }

        return results;
    }

    private async Task<SriResult> ProcessScriptNodeAsync(HtmlNode node, string filePath, string projectRoot)
    {
        var src = node.GetAttributeValue("src", "");
        var isLocal = !src.StartsWith("http", StringComparison.OrdinalIgnoreCase);

        try
        {
            if (!isLocal && !_configuration.IncludeExternalResources)
            {
                return new SriResult(filePath, src, "", ResourceType.JavaScript, false, SriStatus.Skipped);
            }

            if (HasIntegrityAttribute(node) && !_configuration.OverwriteExisting)
            {
                return new SriResult(filePath, src, "", ResourceType.JavaScript, isLocal, SriStatus.AlreadyExists);
            }

            var hash = await GenerateHashAsync(src, projectRoot, isLocal);
            node.SetAttributeValue("integrity", hash);
            node.SetAttributeValue("crossorigin", "anonymous");

            return new SriResult(filePath, src, hash, ResourceType.JavaScript, isLocal, SriStatus.Added);
        }
        catch (Exception ex)
        {
            return new SriResult(filePath, src, "", ResourceType.JavaScript, isLocal, SriStatus.Failed, ex.Message);
        }
    }

    private async Task<SriResult> ProcessLinkNodeAsync(HtmlNode node, string filePath, string projectRoot)
    {
        var href = node.GetAttributeValue("href", "");
        var isLocal = !href.StartsWith("http", StringComparison.OrdinalIgnoreCase);

        try
        {
            if (!isLocal && !_configuration.IncludeExternalResources)
            {
                return new SriResult(filePath, href, "", ResourceType.Stylesheet, false, SriStatus.Skipped);
            }

            if (HasIntegrityAttribute(node) && !_configuration.OverwriteExisting)
            {
                return new SriResult(filePath, href, "", ResourceType.Stylesheet, isLocal, SriStatus.AlreadyExists);
            }

            var hash = await GenerateHashAsync(href, projectRoot, isLocal);
            node.SetAttributeValue("integrity", hash);
            node.SetAttributeValue("crossorigin", "anonymous");

            return new SriResult(filePath, href, hash, ResourceType.Stylesheet, isLocal, SriStatus.Added);
        }
        catch (Exception ex)
        {
            return new SriResult(filePath, href, "", ResourceType.Stylesheet, isLocal, SriStatus.Failed, ex.Message);
        }
    }

    private async Task<string> GenerateHashAsync(string resourceUrl, string projectRoot, bool isLocal)
    {
        byte[] content;

        if (isLocal)
        {
            var resourcePath = Path.Combine(projectRoot, resourceUrl.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(resourcePath))
            {
                throw new FileNotFoundException($"Arquivo não encontrado: {resourcePath}");
            }

            content = await File.ReadAllBytesAsync(resourcePath);
        }
        else
        {
            content = await _httpClient.GetByteArrayAsync(resourceUrl);
        }

        var hashAlgorithmName = _configuration.HashAlgorithm switch
        {
            HashAlgorithmType.Sha256 => "sha256",
            HashAlgorithmType.Sha384 => "sha384",
            HashAlgorithmType.Sha512 => "sha512",
            _ => "sha384"
        };

        using HashAlgorithm hashAlgorithm = _configuration.HashAlgorithm switch
        {
            HashAlgorithmType.Sha256 => SHA256.Create(),
            HashAlgorithmType.Sha384 => SHA384.Create(),
            HashAlgorithmType.Sha512 => SHA512.Create(),
            _ => SHA384.Create()
        };

        var hash = hashAlgorithm.ComputeHash(content);
        var base64Hash = Convert.ToBase64String(hash);

        return $"{hashAlgorithmName}-{base64Hash}";
    }

    private static bool HasIntegrityAttribute(HtmlNode node)
    {
        return node.GetAttributeValue("integrity", "") != "";
    }

    private void CreateBackup(string filePath)
    {
        var backupPath = $"{filePath}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
        File.Copy(filePath, backupPath);
    }

    private IEnumerable<string> GetHtmlFiles(string projectPath)
    {
        var patterns = new[] { "*.html", "*.htm", "*.aspx", "*.master", "*.ascx" };

        return patterns
            .SelectMany(pattern => Directory.GetFiles(projectPath, pattern, SearchOption.AllDirectories))
            .Where(file => !ShouldExcludeFile(file))
            .Distinct();
    }

    private bool ShouldExcludeFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return _configuration.ExcludePatterns.Any(pattern =>
            fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}