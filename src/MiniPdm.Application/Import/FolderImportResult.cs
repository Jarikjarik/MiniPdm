namespace MiniPdm.Application.Import;

public sealed record FolderImportResult(
    CadFolderReadResult ReadResult,
    CadValidationResult ValidationResult,
    CadPersistenceImportResult PersistenceResult,
    ImportReport Report);