﻿using System.Data.Entity;
using System.Linq;
using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;

namespace Data.Infrastructure.AutoMapper
{
    /// <summary>
    /// AutoMapper converter to convert RouteDO to FullRouteDTO.
    /// </summary>
    public class RouteDOFullConverter
        : ITypeConverter<RouteDO, RouteDTO>
    {
        public const string UnitOfWork_OptionsKey = "UnitOfWork";


        public RouteDTO Convert(ResolutionContext context)
        {
            var route = (RouteDO)context.SourceValue;
            var uow = (IUnitOfWork)context.Options.Items[UnitOfWork_OptionsKey];

            if (route == null)
            {
                return null;
            }

            var subrouteDTOList = uow.SubrouteRepository
                .GetQuery()
                .Include(x => x.RouteNodes)
                .Where(x => x.ParentRouteNodeId == route.Id)
                .OrderBy(x => x.Id)
                .ToList()
                .Select((SubrouteDO x) =>
                {
                    var pntDTO = Mapper.Map<FullSubrouteDTO>(x);
                    pntDTO.Actions = x.RouteNodes.OfType<ActionDO>().Select(Mapper.Map<ActionDTO>).ToList();
                    return pntDTO;
                }).ToList();

            var result = Mapper.Map<RouteDTO>(Mapper.Map<RouteOnlyDTO>(route));
            result.Subroutes = subrouteDTOList;

            return result;
        }
    }
}
