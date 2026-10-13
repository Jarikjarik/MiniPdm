using MiniPdm.Application.Bom;
using MiniPdm.Application.Bom.Models;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Tests.Bom;

public class BomCalculationServiceTests
{
    [Fact]
    public async Task CalculateAsync_NestedAssembly_CalculatesMassAndSpecification()
    {
        var root = CreateAssembly(
            1,
            "ТЕСТ.100000.001",
            "Редуктор");

        var nodes = new[]
        {
            new BomTreeNode(
                1,
                101,
                2,
                102,
                ObjectType.Assembly,
                "ТЕСТ.100000.002",
                "Вал в сборе",
                VersionState.InWork,
                null,
                null,
                2,
                2,
                1,
                false),

            new BomTreeNode(
                2,
                102,
                3,
                103,
                ObjectType.Part,
                "ТЕСТ.100000.003",
                "Подшипник",
                VersionState.InWork,
                "Сталь",
                2.50m,
                3,
                6,
                2,
                false),

            new BomTreeNode(
                1,
                101,
                4,
                104,
                ObjectType.StandardPart,
                null,
                "Болт М10",
                VersionState.InWork,
                null,
                0.20m,
                4,
                4,
                1,
                false)
        };

        var service = CreateService(
            new BomStructure(
                root,
                nodes));

        var result =
            await service.CalculateAsync(
                root.ObjectId,
                CancellationToken.None);

        Assert.False(
            result.HasIssues);

        Assert.True(
            result.HasMass);

        Assert.Equal(
            15.80m,
            result.MassKg);

        Assert.Equal(
            2,
            result.Specification.Count);

        var bearing =
            result.Specification.Single(
                item =>
                    item.ObjectId == 3);

        Assert.Equal(
            6,
            bearing.Quantity);

        Assert.Equal(
            2.50m,
            bearing.MassKg);

        var bolt =
            result.Specification.Single(
                item =>
                    item.ObjectId == 4);

        Assert.Equal(
            4,
            bolt.Quantity);

        Assert.Equal(
            0.20m,
            bolt.MassKg);
    }

    [Fact]
    public async Task CalculateAsync_SameLeafThroughDifferentBranches_SumsQuantities()
    {
        var root = CreateAssembly(
            10,
            "ТЕСТ.100001.001",
            "Главная сборка");

        var nodes = new[]
        {
            new BomTreeNode(
                10,
                201,
                11,
                202,
                ObjectType.Assembly,
                "ТЕСТ.100001.002",
                "Сборка А",
                VersionState.InWork,
                null,
                null,
                1,
                1,
                1,
                false),

            new BomTreeNode(
                10,
                201,
                12,
                203,
                ObjectType.Assembly,
                "ТЕСТ.100001.003",
                "Сборка Б",
                VersionState.InWork,
                null,
                null,
                1,
                1,
                1,
                false),

            new BomTreeNode(
                11,
                202,
                13,
                204,
                ObjectType.Part,
                "ТЕСТ.100001.004",
                "Общая деталь",
                VersionState.InWork,
                "Сталь",
                1.50m,
                2,
                2,
                2,
                false),

            new BomTreeNode(
                12,
                203,
                13,
                204,
                ObjectType.Part,
                "ТЕСТ.100001.004",
                "Общая деталь",
                VersionState.InWork,
                "Сталь",
                1.50m,
                3,
                3,
                2,
                false)
        };

        var service = CreateService(
            new BomStructure(
                root,
                nodes));

        var result =
            await service.CalculateAsync(
                root.ObjectId,
                CancellationToken.None);

        Assert.False(
            result.HasIssues);

        Assert.Equal(
            7.50m,
            result.MassKg);

        var item =
            Assert.Single(
                result.Specification);

        Assert.Equal(
            13,
            item.ObjectId);

        Assert.Equal(
            5,
            item.Quantity);
    }

    [Fact]
    public async Task CalculateAsync_LeafWithoutMass_ReturnsIssueAndSpecification()
    {
        var root = CreateAssembly(
            20,
            "ТЕСТ.100002.001",
            "Сборка");

        var nodes = new[]
        {
            new BomTreeNode(
                20,
                301,
                21,
                302,
                ObjectType.Part,
                "ТЕСТ.100002.002",
                "Прокладка",
                VersionState.InWork,
                "Паронит",
                null,
                2,
                2,
                1,
                false)
        };

        var service = CreateService(
            new BomStructure(
                root,
                nodes));

        var result =
            await service.CalculateAsync(
                root.ObjectId,
                CancellationToken.None);

        Assert.False(
            result.HasMass);

        Assert.Null(
            result.MassKg);

        var issue =
            Assert.Single(
                result.Issues);

        Assert.Equal(
            BomCalculationIssueCode.MissingMass,
            issue.Code);

        Assert.Equal(
            21,
            issue.ObjectId);

        var specificationItem =
            Assert.Single(
                result.Specification);

        Assert.Equal(
            2,
            specificationItem.Quantity);

        Assert.Null(
            specificationItem.MassKg);
    }

    [Fact]
    public async Task CalculateAsync_ChildWithoutCurrentVersion_ReturnsIssue()
    {
        var root = CreateAssembly(
            30,
            "ТЕСТ.100003.001",
            "Сборка");

        var nodes = new[]
        {
            new BomTreeNode(
                30,
                401,
                31,
                null,
                ObjectType.Part,
                "ТЕСТ.100003.002",
                "Отменённая деталь",
                null,
                null,
                null,
                1,
                1,
                1,
                false)
        };

        var service = CreateService(
            new BomStructure(
                root,
                nodes));

        var result =
            await service.CalculateAsync(
                root.ObjectId,
                CancellationToken.None);

        Assert.Null(
            result.MassKg);

        Assert.Empty(
            result.Specification);

        var issue =
            Assert.Single(
                result.Issues);

        Assert.Equal(
            BomCalculationIssueCode.MissingCurrentVersion,
            issue.Code);

        Assert.Equal(
            31,
            issue.ObjectId);
    }

    [Fact]
    public async Task CalculateAsync_CycleDetected_ReturnsIssue()
    {
        var root = CreateAssembly(
            40,
            "ТЕСТ.100004.001",
            "Сборка А");

        var nodes = new[]
        {
            new BomTreeNode(
                40,
                501,
                41,
                502,
                ObjectType.Assembly,
                "ТЕСТ.100004.002",
                "Сборка Б",
                VersionState.InWork,
                null,
                null,
                1,
                1,
                1,
                false),

            new BomTreeNode(
                41,
                502,
                40,
                501,
                ObjectType.Assembly,
                "ТЕСТ.100004.001",
                "Сборка А",
                VersionState.InWork,
                null,
                null,
                1,
                1,
                2,
                true)
        };

        var service = CreateService(
            new BomStructure(
                root,
                nodes));

        var result =
            await service.CalculateAsync(
                root.ObjectId,
                CancellationToken.None);

        Assert.Null(
            result.MassKg);

        Assert.Empty(
            result.Specification);

        var issue =
            Assert.Single(
                result.Issues);

        Assert.Equal(
            BomCalculationIssueCode.CycleDetected,
            issue.Code);
    }

    [Fact]
    public async Task CalculateAsync_PartRoot_ReturnsOwnMassAndQuantityOne()
    {
        var root = new BomObject(
            50,
            ObjectType.Part,
            "ТЕСТ.100005.001",
            "Вал",
            601,
            1,
            VersionState.InWork,
            "Сталь 45",
            3.25m);

        var service = CreateService(
            new BomStructure(
                root,
                Array.Empty<BomTreeNode>()));

        var result =
            await service.CalculateAsync(
                root.ObjectId,
                CancellationToken.None);

        Assert.False(
            result.HasIssues);

        Assert.Equal(
            3.25m,
            result.MassKg);

        var item =
            Assert.Single(
                result.Specification);

        Assert.Equal(
            root.ObjectId,
            item.ObjectId);

        Assert.Equal(
            1,
            item.Quantity);
    }

    [Fact]
    public async Task CalculateAsync_RootWithoutCurrentVersion_ReturnsIssue()
    {
        var root = new BomObject(
            60,
            ObjectType.Part,
            "ТЕСТ.100006.001",
            "Отменённая деталь",
            null,
            null,
            null,
            null,
            null);

        var service = CreateService(
            new BomStructure(
                root,
                Array.Empty<BomTreeNode>()));

        var result =
            await service.CalculateAsync(
                root.ObjectId,
                CancellationToken.None);

        Assert.Null(
            result.MassKg);

        Assert.Empty(
            result.Specification);

        var issue =
            Assert.Single(
                result.Issues);

        Assert.Equal(
            BomCalculationIssueCode.MissingCurrentVersion,
            issue.Code);

        Assert.Equal(
            root.ObjectId,
            issue.ObjectId);
    }

    [Fact]
    public async Task CalculateAsync_UnknownObject_ReturnsObjectNotFoundIssue()
    {
        var service =
            new BomCalculationService(
                new FakeBomStructureReader(
                    null));

        var result =
            await service.CalculateAsync(
                999,
                CancellationToken.None);

        Assert.Null(
            result.MassKg);

        Assert.Empty(
            result.Specification);

        var issue =
            Assert.Single(
                result.Issues);

        Assert.Equal(
            BomCalculationIssueCode.ObjectNotFound,
            issue.Code);

        Assert.Equal(
            999,
            issue.ObjectId);
    }

    [Fact]
    public async Task CalculateAsync_EmptyAssembly_ReturnsZeroMassAndEmptySpecification()
    {
        var root = CreateAssembly(
            70,
            "ТЕСТ.100007.001",
            "Пустая сборка");

        var service = CreateService(
            new BomStructure(
                root,
                Array.Empty<BomTreeNode>()));

        var result =
            await service.CalculateAsync(
                root.ObjectId,
                CancellationToken.None);

        Assert.False(
            result.HasIssues);

        Assert.True(
            result.HasMass);

        Assert.Equal(
            0m,
            result.MassKg);

        Assert.Empty(
            result.Specification);
    }

    private static BomCalculationService CreateService(
        BomStructure structure)
    {
        return new BomCalculationService(
            new FakeBomStructureReader(
                structure));
    }

    private static BomObject CreateAssembly(
        long objectId,
        string designation,
        string name)
    {
        return new BomObject(
            objectId,
            ObjectType.Assembly,
            designation,
            name,
            objectId + 100,
            1,
            VersionState.InWork,
            null,
            null);
    }

    private sealed class FakeBomStructureReader
        : IBomStructureReader
    {
        private readonly BomStructure? _structure;

        public FakeBomStructureReader(
            BomStructure? structure)
        {
            _structure = structure;
        }

        public Task<BomStructure?> ReadAsync(
            long rootObjectId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                _structure);
        }
    }
}