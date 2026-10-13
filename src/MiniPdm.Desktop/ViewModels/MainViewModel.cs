using System.Collections.ObjectModel;
using MiniPdm.Application.Bom;
using MiniPdm.Application.Bom.Models;
using MiniPdm.Application.Browse;
using MiniPdm.Application.Browse.Models;
using MiniPdm.Application.Import;
using MiniPdm.Application.Versions;
using MiniPdm.Desktop.Commands;
using MiniPdm.Desktop.Services;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IPdmCatalogReader _catalogReader;
    private readonly IFolderPickerService _folderPickerService;
    private readonly FolderImportService _folderImportService;
    private readonly VersionStateService _versionStateService;
    private readonly IBomStructureReader _bomStructureReader;
    private readonly BomCalculationService _bomCalculationService;

    private string _searchText = string.Empty;
    private string _statusMessage = "Готово.";
    private bool _isBusy;

    private PdmObjectListItem? _selectedObject;
    private PdmObjectDetails? _selectedDetails;
    private BomStructure? _selectedBomStructure;
    private decimal? _calculatedMassKg;
    private string _calculationIssues = string.Empty;

    public MainViewModel(
        IPdmCatalogReader catalogReader,
        IFolderPickerService folderPickerService,
        FolderImportService folderImportService,
        VersionStateService versionStateService,
        IBomStructureReader bomStructureReader,
        BomCalculationService bomCalculationService)
    {
        _catalogReader = catalogReader
            ?? throw new ArgumentNullException(
                nameof(catalogReader));

        _folderPickerService = folderPickerService
            ?? throw new ArgumentNullException(
                nameof(folderPickerService));

        _folderImportService = folderImportService
            ?? throw new ArgumentNullException(
                nameof(folderImportService));

        _versionStateService = versionStateService
            ?? throw new ArgumentNullException(
                nameof(versionStateService));

        _bomStructureReader = bomStructureReader
            ?? throw new ArgumentNullException(
                nameof(bomStructureReader));

        _bomCalculationService = bomCalculationService
            ?? throw new ArgumentNullException(
                nameof(bomCalculationService));

        SearchCommand =
            new AsyncRelayCommand(
                SearchAsync,
                () => !IsBusy);

        RefreshCommand =
            new AsyncRelayCommand(
                RefreshAsync,
                () => !IsBusy);

        ImportFolderCommand =
            new AsyncRelayCommand(
                ImportFolderAsync,
                () => !IsBusy);

        ApproveCommand =
            new AsyncRelayCommand(
                ApproveAsync,
                CanApprove);

        CancelCommand =
            new AsyncRelayCommand(
                CancelAsync,
                CanCancel);
    }

    public ObservableCollection<PdmObjectListItem> Objects { get; } = [];

    public ObservableCollection<ImportReportItemViewModel>
        ImportReportItems
    { get; } = [];

    public ObservableCollection<FlatSpecificationItem>
        SpecificationItems
    { get; } = [];

    public ObservableCollection<BomTreeItemViewModel>
        BomTreeItems
    { get; } = [];

    public AsyncRelayCommand SearchCommand { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand ImportFolderCommand { get; }

    public AsyncRelayCommand ApproveCommand { get; }

    public AsyncRelayCommand CancelCommand { get; }

    public string SearchText
    {
        get => _searchText;

        set => SetProperty(
            ref _searchText,
            value);
    }

    public string StatusMessage
    {
        get => _statusMessage;

        private set => SetProperty(
            ref _statusMessage,
            value);
    }

    public bool IsBusy
    {
        get => _isBusy;

        private set
        {
            if (!SetProperty(
                    ref _isBusy,
                    value))
            {
                return;
            }

            RaiseCommandCanExecuteChanged();
        }
    }

    public PdmObjectListItem? SelectedObject
    {
        get => _selectedObject;

        set
        {
            if (!SetProperty(
                    ref _selectedObject,
                    value))
            {
                return;
            }

            ClearSelectedObjectData();

            _ = LoadSelectedObjectAsync();
        }
    }

    public PdmObjectDetails? SelectedDetails
    {
        get => _selectedDetails;

        private set
        {
            if (!SetProperty(
                    ref _selectedDetails,
                    value))
            {
                return;
            }

            ApproveCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
        }
    }

    public BomStructure? SelectedBomStructure
    {
        get => _selectedBomStructure;

        private set => SetProperty(
            ref _selectedBomStructure,
            value);
    }

    public decimal? CalculatedMassKg
    {
        get => _calculatedMassKg;

        private set => SetProperty(
            ref _calculatedMassKg,
            value);
    }

    public string CalculationIssues
    {
        get => _calculationIssues;

        private set => SetProperty(
            ref _calculationIssues,
            value);
    }

    public async Task InitializeAsync()
    {
        await LoadObjectsAsync(
            null);
    }

    private async Task SearchAsync()
    {
        await LoadObjectsAsync(
            SearchText);
    }

    private async Task RefreshAsync()
    {
        await LoadObjectsAsync(
            SearchText,
            SelectedObject?.ObjectId);
    }

    private async Task ImportFolderAsync()
    {
        var folder =
            _folderPickerService.PickFolder();

        if (folder is null)
        {
            StatusMessage =
                "Выбор папки отменён.";

            return;
        }

        try
        {
            IsBusy = true;

            StatusMessage =
                $"Импорт из папки: {folder}...";

            var preferredObjectId =
                SelectedObject?.ObjectId;

            var result =
                await _folderImportService.ImportAsync(
                    folder,
                    CancellationToken.None);

            ImportReportItems.Clear();

            foreach (var item in result.Report.Items)
            {
                ImportReportItems.Add(
                    new ImportReportItemViewModel(
                        item));
            }

            await LoadObjectsAsync(
                SearchText,
                preferredObjectId);

            StatusMessage =
                $"Импорт завершён. " +
                $"Принято: {result.Report.AcceptedCount}, " +
                $"отклонено: {result.Report.RejectedCount}, " +
                $"предупреждений: {result.Report.WarningCount}. " +
                $"Создано: {result.PersistenceResult.CreatedCount}, " +
                $"обновлено: {result.PersistenceResult.UpdatedCount}, " +
                $"новых версий: {result.PersistenceResult.NewVersionCount}, " +
                $"без изменений: {result.PersistenceResult.UnchangedCount}.";
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Ошибка импорта: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApproveAsync()
    {
        await ChangeCurrentVersionStateAsync(
            VersionState.Approved);
    }

    private async Task CancelAsync()
    {
        await ChangeCurrentVersionStateAsync(
            VersionState.Cancelled);
    }

    private bool CanApprove()
    {
        return !IsBusy &&
               SelectedDetails?.CurrentVersion?.State ==
               VersionState.InWork;
    }

    private bool CanCancel()
    {
        if (IsBusy)
        {
            return false;
        }

        var state =
            SelectedDetails?.CurrentVersion?.State;

        return state is
            VersionState.InWork or
            VersionState.Approved;
    }

    private async Task ChangeCurrentVersionStateAsync(
        VersionState newState)
    {
        var details =
            SelectedDetails;

        var currentVersion =
            details?.CurrentVersion;

        if (details is null ||
            currentVersion is null)
        {
            StatusMessage =
                "У объекта нет текущей версии.";

            return;
        }

        var objectId =
            details.ObjectId;

        try
        {
            IsBusy = true;

            StatusMessage =
                $"Изменение состояния версии " +
                $"{currentVersion.VersionNumber}...";

            await _versionStateService.ChangeStateAsync(
                currentVersion.VersionId,
                newState,
                CancellationToken.None);

            await LoadObjectsAsync(
                SearchText,
                objectId);

            StatusMessage =
                $"Версия {currentVersion.VersionNumber} " +
                $"переведена в состояние " +
                $"«{UiTextFormatter.FormatVersionState(newState)}».";
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Ошибка смены состояния: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadObjectsAsync(
        string? searchText,
        long? preferredObjectId = null)
    {
        try
        {
            IsBusy = true;

            StatusMessage =
                "Загрузка объектов...";

            var items =
                await _catalogReader.SearchAsync(
                    searchText,
                    CancellationToken.None);

            Objects.Clear();

            foreach (var item in items)
            {
                Objects.Add(
                    item);
            }

            PdmObjectListItem? nextSelectedObject = null;

            if (Objects.Count > 0)
            {
                nextSelectedObject =
                    preferredObjectId.HasValue
                        ? Objects.FirstOrDefault(
                              item =>
                                  item.ObjectId ==
                                  preferredObjectId.Value)
                          ?? Objects[0]
                        : Objects[0];
            }

            SetProperty(
                ref _selectedObject,
                nextSelectedObject,
                nameof(SelectedObject));

            ClearSelectedObjectData();

            if (nextSelectedObject is not null)
            {
                await LoadSelectedObjectAsync();
            }

            StatusMessage =
                $"Объектов найдено: {Objects.Count}.";
        }
        catch (Exception exception)
        {
            Objects.Clear();

            SetProperty(
                ref _selectedObject,
                null,
                nameof(SelectedObject));

            ClearSelectedObjectData();

            StatusMessage =
                $"Ошибка: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSelectedObjectAsync()
    {
        if (SelectedObject is null)
        {
            ClearSelectedObjectData();

            return;
        }

        var objectId =
            SelectedObject.ObjectId;

        try
        {
            var details =
                await _catalogReader.GetDetailsAsync(
                    objectId,
                    CancellationToken.None);

            var bomStructure =
                await _bomStructureReader.ReadAsync(
                    objectId,
                    CancellationToken.None);

            var calculation =
                await _bomCalculationService.CalculateAsync(
                    objectId,
                    CancellationToken.None);

            if (SelectedObject?.ObjectId != objectId)
            {
                return;
            }

            SelectedDetails =
                details;

            SelectedBomStructure =
                bomStructure;

            BuildBomTree(
                bomStructure);

            CalculatedMassKg =
                calculation.MassKg;

            SpecificationItems.Clear();

            foreach (var item in calculation.Specification)
            {
                SpecificationItems.Add(
                    item);
            }

            CalculationIssues =
                string.Join(
                    Environment.NewLine,
                    calculation.Issues.Select(
                        FormatCalculationIssue));
        }
        catch (Exception exception)
        {
            ClearSelectedObjectData();

            StatusMessage =
                $"Ошибка загрузки объекта: {exception.Message}";
        }
    }

    private void BuildBomTree(
        BomStructure? structure)
    {
        BomTreeItems.Clear();

        if (structure is null)
        {
            return;
        }

        var root =
            new BomTreeItemViewModel(
                structure.Root.ObjectId,
                structure.Root.ObjectType,
                structure.Root.Designation,
                structure.Root.Name,
                1,
                1,
                false);

        BomTreeItems.Add(
            root);

        BuildBomChildren(
            root,
            structure.Nodes,
            new HashSet<long>
            {
                structure.Root.ObjectId
            });
    }

    private static void BuildBomChildren(
        BomTreeItemViewModel parent,
        IReadOnlyList<BomTreeNode> nodes,
        HashSet<long> currentPath)
    {
        var children = nodes
            .Where(node =>
                node.ParentObjectId ==
                parent.ObjectId)
            .GroupBy(node =>
                node.ObjectId)
            .Select(group =>
                group.First())
            .OrderBy(node =>
                node.ObjectType)
            .ThenBy(node =>
                node.Designation)
            .ThenBy(node =>
                node.Name)
            .ToArray();

        foreach (var node in children)
        {
            var isCycle =
                node.IsCycle ||
                currentPath.Contains(
                    node.ObjectId);

            var child =
                new BomTreeItemViewModel(
                    node.ObjectId,
                    node.ObjectType,
                    node.Designation,
                    node.Name,
                    node.Quantity,
                    node.TotalQuantity,
                    isCycle);

            parent.Children.Add(
                child);

            if (isCycle)
            {
                continue;
            }

            var childPath =
                new HashSet<long>(
                    currentPath)
                {
                    node.ObjectId
                };

            BuildBomChildren(
                child,
                nodes,
                childPath);
        }
    }

    private static string FormatCalculationIssue(
        BomCalculationIssue issue)
    {
        var objectName =
            string.IsNullOrWhiteSpace(issue.Name)
                ? $"ID {issue.ObjectId}"
                : $"«{issue.Name}»";

        return issue.Code switch
        {
            BomCalculationIssueCode.ObjectNotFound =>
                $"Объект {objectName} не найден.",

            BomCalculationIssueCode.MissingCurrentVersion =>
                $"У объекта {objectName} нет текущей версии.",

            BomCalculationIssueCode.MissingMass =>
                $"Для объекта {objectName} не указана масса.",

            BomCalculationIssueCode.CycleDetected =>
                $"Обнаружен цикл в составе на объекте {objectName}.",

            _ =>
                issue.Message
        };
    }

    private void ClearSelectedObjectData()
    {
        SelectedDetails = null;
        SelectedBomStructure = null;
        CalculatedMassKg = null;
        CalculationIssues = string.Empty;

        SpecificationItems.Clear();
        BomTreeItems.Clear();
    }

    private void RaiseCommandCanExecuteChanged()
    {
        SearchCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
        ImportFolderCommand.RaiseCanExecuteChanged();
        ApproveCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
    }
}