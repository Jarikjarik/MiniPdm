using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MiniPdm.Application.Bom;
using MiniPdm.Application.Import;
using MiniPdm.Application.Versions;
using MiniPdm.Desktop.Services;
using MiniPdm.Desktop.ViewModels;
using MiniPdm.Infrastructure.Cad;
using MiniPdm.Infrastructure.Persistence;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Queries;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.Desktop;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        RenderOptions.ProcessRenderMode =
            RenderMode.SoftwareOnly;

        var connectionString =
            Environment.GetEnvironmentVariable(
                "MINIPDM_CONNECTION_STRING")
            ??
            "Host=localhost;Port=5433;Database=minipdm;" +
            "Username=minipdm;Password=minipdm_dev";

        var connectionFactory =
            new NpgsqlConnectionFactory(
                connectionString);

        var catalogReader =
            new PostgresPdmCatalogReader(
                connectionFactory);

        var folderPickerService =
            new FolderPickerService();

        var documentLocator =
            new FileSystemCadDocumentLocator();

        var documentReader =
            new JsonCadDocumentReader();

        var folderReader =
            new CadFolderReader(
                documentLocator,
                documentReader);

        var validator =
            new CadDocumentValidator();

        var unitOfWorkFactory =
            new PdmImportUnitOfWorkFactory(
                connectionFactory);

        var persistenceImporter =
            new CadImportService(
                unitOfWorkFactory);

        var reportBuilder =
            new ImportReportBuilder();

        var folderImportService =
            new FolderImportService(
                folderReader,
                validator,
                persistenceImporter,
                reportBuilder);

        var versionStateService =
            new VersionStateService(
                unitOfWorkFactory);

        var bomStructureReader =
            new PostgresBomStructureReader(
                connectionFactory,
                new BomLinkRepository());

        var bomCalculationService =
            new BomCalculationService(
                bomStructureReader);

        var viewModel =
            new MainViewModel(
                catalogReader,
                folderPickerService,
                folderImportService,
                versionStateService,
                bomStructureReader,
                bomCalculationService);

        var window =
            new MainWindow
            {
                DataContext = viewModel
            };

        MainWindow = window;

        window.Show();

        await viewModel.InitializeAsync();
    }
}