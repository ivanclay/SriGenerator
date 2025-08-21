### Subresource Integrity (SRI) Generator

This project, **SriGenerator**, is a tool designed to automate the generation and insertion of Subresource Integrity (SRI) attributes into HTML, ASPX, and similar files. SRI is a security feature that enables browsers to verify that the files they fetch (for example, from a CDN) have been delivered without unexpected manipulation.

The tool supports generating hashes for both JavaScript and CSS stylesheet files, covering both local and external resources, such as those hosted on CDNs.

### Features

  * **File Processing**: The tool scans a project directory for HTML, HTM, ASPX, and master/ascx files, and processes all `script` and `link` resources found within them.
  * **Local and External Resource Support**: The generator can be configured to include or exclude external resources.
  * **Multiple Hash Algorithms**: It supports the SHA-256, SHA-384, and SHA-512 hash algorithms. The default is SHA-384.
  * **Backups**: It creates a backup of the original file before modifying it to add the integrity attribute.
  * **Overwrite Control**: The tool allows control over whether existing SRI hashes should be overwritten.
  * **Processing Report**: After execution, it generates a detailed report with the number of files processed, successful updates, failures, and the total processing time.

### How to Use

The `SriGenerator` class is the main facade for interacting with the tool.

#### Basic Usage Example

To process an entire project with the default configuration, use the following approach:

```csharp
using SriGenerator;

var generator = new SriGenerator();
var result = await generator.ProcessProjectAsync("path/to/your/project");

Console.WriteLine($"Files Processed: {result.TotalFilesProcessed}");
Console.WriteLine($"Successful Updates: {result.SuccessfulUpdates}");
Console.WriteLine($"Failures: {result.Failures}");
Console.WriteLine($"Processing Time: {result.ProcessingTime}");

foreach (var sriResult in result.Results)
{
    Console.WriteLine($"Resource: {sriResult.ResourceUrl} - Status: {sriResult.Status}");
}
```

#### Factory Methods

The `SriGenerator` class provides static factory methods to create pre-configured instances.

  * `SriGenerator.CreateDefault()`: Creates an instance with the default configuration (SHA-384, does not include external resources, creates a backup).
  * `SriGenerator.CreateForLocalResourcesOnly()`: Configures the instance to process only local files, using the SHA-384 algorithm and creating backups.
  * `SriGenerator.CreateForAllResources()`: Configures the instance to process all resources, including external ones, using SHA-384 and creating backups.

#### Configuration Options

The `SriConfiguration` class allows you to fine-tune the tool's behavior. You can create an instance and pass it to the `SriGenerator` constructor.

```csharp
var config = new SriConfiguration(
    HashAlgorithm: HashAlgorithmType.Sha512,
    IncludeExternalResources: true,
    CreateBackup: false,
    OverwriteExisting: true,
    ExcludePatterns: new [] { "jquery.js" }
);

var generator = new SriGenerator(config);
await generator.ProcessProjectAsync("path/to/your/project");
```

### Implementation Details

  * **SriGenerator.cs**: The main facade that manages the service lifecycle and exposes the processing methods.
  * **SriGeneratorService.cs**: Contains the core logic for file scanning, HTML analysis, and hash generation. It uses the `HtmlAgilityPack` library to manipulate the HTML DOM.
  * **SriConfiguration.cs**: Defines the configuration options, such as the hash algorithm and whether external resources should be included.
  * **Models**:
      * `HashAlgorithmType.cs`: An enumeration for the supported hash algorithms (SHA-256, SHA-384, SHA-512).
      * `ResourceType.cs`: An enumeration that identifies the type of resource (JavaScript, Stylesheet, Unknown).
      * `SriStatus.cs`: An enumeration for a resource's processing status (Added, Updated, Skipped, Failed, AlreadyExists).
      * `SriResult.cs`: A record that holds the processing result details for a single resource.
      * `ProcessingResult.cs`: A record that summarizes the result of a project processing, including statistics and a list of individual results.