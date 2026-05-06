using LimsProject.Common.Persistence;
using LimsProject.Common.Results;
using LimsProject.Common.Serialization;
using LimsProject.Features.Batches;
using LimsProject.Features.Traceability;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.Features.Packaging;

public sealed class PackagingService(
    AppDbContext db,
    IBatchTransitionService transitions,
    IChainOfCustodyWriter coc) : IPackagingService
{
    public async Task<Result<FinishedProductResponse>> CreateFinishedProductAsync(CreateFinishedProductRequest request, CancellationToken ct)
    {
        var strainExists = await db.Strains.AsNoTracking().AnyAsync(s => s.Id == request.StrainId, ct);
        if (!strainExists)
            return Result<FinishedProductResponse>.Failure("Strain não encontrada.");

        var p = new FinishedProduct
        {
            Name = request.Name.Trim(),
            StrainId = request.StrainId,
            UnitWeightGrams = request.UnitWeightGrams,
            Format = request.Format,
        };
        db.FinishedProducts.Add(p);
        await db.SaveChangesAsync(ct);

        return Result<FinishedProductResponse>.Success(new FinishedProductResponse(
            p.Id, p.Name, p.StrainId, p.UnitWeightGrams, p.Format, p.CreatedAt));
    }

    public async Task<Result<IReadOnlyList<ProductPackageResponse>>> PackBatchAsync(Guid batchId, PackBatchRequest request, CancellationToken ct)
    {
        if (request.UnitCount <= 0 || request.UnitCount > 10_000)
            return Result<IReadOnlyList<ProductPackageResponse>>.Failure("Quantidade de unidades inválida.");

        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<IReadOnlyList<ProductPackageResponse>>.Failure("Lote não encontrado.");

        if (batch.Status != BatchStatus.Released)
            return Result<IReadOnlyList<ProductPackageResponse>>.Failure("Somente lotes Released podem ser empacotados.");

        var product = await db.FinishedProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.FinishedProductId, ct);
        if (product is null)
            return Result<IReadOnlyList<ProductPackageResponse>>.Failure("Produto não encontrado.");

        var check = transitions.ValidateTransition(batch.Status, BatchStatus.Packaged);
        if (!check.IsSuccess)
            return Result<IReadOnlyList<ProductPackageResponse>>.Failure(check.Error!);

        var packages = new List<ProductPackage>(request.UnitCount);
        for (var i = 0; i < request.UnitCount; i++)
        {
            packages.Add(new ProductPackage
            {
                BatchId = batchId,
                FinishedProductId = request.FinishedProductId,
                SerialNumber = SerialNumberGenerator.Create(),
                WeightGrams = request.UnitWeightGrams,
                PackagedAt = DateTime.UtcNow,
            });
        }

        db.ProductPackages.AddRange(packages);
        batch.Status = BatchStatus.Packaged;
        coc.Append(batchId, CocEventTypes.PackagingCreated, new { request.FinishedProductId, request.UnitCount }, null);
        await db.SaveChangesAsync(ct);

        IReadOnlyList<ProductPackageResponse> responses = packages
            .Select(pkg => new ProductPackageResponse(
                pkg.Id,
                pkg.SerialNumber,
                $"lims:{pkg.SerialNumber}",
                pkg.WeightGrams,
                pkg.PackagedAt))
            .ToList();

        return Result<IReadOnlyList<ProductPackageResponse>>.Success(responses);
    }

    public async Task<Result> MarkPackageSoldAsync(string serialNumber, CancellationToken ct)
    {
        var pkg = await db.ProductPackages.FirstOrDefaultAsync(p => p.SerialNumber == serialNumber, ct);
        if (pkg is null)
            return Result.Failure("Pacote não encontrado.");

        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == pkg.BatchId, ct);
        if (batch is null)
            return Result.Failure("Lote não encontrado.");

        var check = transitions.ValidateTransition(batch.Status, BatchStatus.Sold);
        if (!check.IsSuccess)
            return Result.Failure(check.Error!);

        pkg.IsSold = true;
        batch.Status = BatchStatus.Sold;
        coc.Append(batch.Id, CocEventTypes.PackageSold, new { serialNumber }, null);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
