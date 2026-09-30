using Mapster;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Contracts.Common;

namespace Sporeo.Fixtures.Api.Mappings;

internal sealed class PagedResponseMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType(typeof(PagedResult<>), typeof(PagedResponse<>))
            .MapToConstructor(true)
            .Map("PageNumber", "Pagination.PageNumber")
            .Map("PageSize", "Pagination.PageSize");
    }
}
