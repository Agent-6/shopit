using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Products.Commands.ChangeProductStatus;

/// <summary>
/// Moves a product to a lifecycle status in the acting tenant.
/// </summary>
/// <remarks>
/// <para>
/// One command with a target status rather than <c>ActivateProduct</c> and <c>ArchiveProduct</c>: the
/// lifecycle's rule is about <em>transitions</em>, and splitting it would put half of that rule in each
/// handler while leaving the HTTP surface with two routes that do the same thing with a different constant.
/// The aggregate decides what is allowed (Draft to Active or Archived, Active to Archived, Archived
/// terminal), so adding a transition later is one edit in one place.
/// </para>
/// <para>
/// Sending the status a product already has is a successful no-op — the endpoint is idempotent, so a client
/// can send the state it wants without reading first.
/// </para>
/// </remarks>
/// <param name="Id">The product whose status changes.</param>
/// <param name="Status">The status to move to.</param>
public record ChangeProductStatusCommand(
    Guid Id,
    ProductStatus Status) : ICommand<Result<ChangeProductStatusResult, Error>>;
