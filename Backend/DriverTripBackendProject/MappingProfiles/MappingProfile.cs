using AutoMapper;
using DriverTripBackendProject.DTO.Trips;
using DriverTripBackendProject.Models;

namespace DriverTripBackendProject.MappingProfiles
{
    public class MappingProfile :Profile
    {
        public MappingProfile()
        {
            // Map DTO to Entity and vice versa
            CreateMap<TripDTO, Trip>();
            CreateMap<Trip, TripDTO>();
            CreateMap<TripUpdateDTO, Trip>();


            // GET mapping
            CreateMap<Trip, TripResponseDTO>()
              .ForMember(dest => dest.OriginCityName, opt => opt.MapFrom(src => src.OriginCity.Name))
              .ForMember(dest => dest.OriginCityId, opt => opt.MapFrom(src => src.OriginCityId))
              .ForMember(dest => dest.OriginAreaName, opt => opt.MapFrom(src => src.OriginArea.Name))
              .ForMember(dest => dest.OriginAreaId, opt => opt.MapFrom(src => src.OriginAreaId))
              .ForMember(dest => dest.DestinationCityName, opt => opt.MapFrom(src => src.DestinationCity.Name))
              .ForMember(dest => dest.DestinationCityId, opt => opt.MapFrom(src => src.DestinationCityId))
              .ForMember(dest => dest.DestinationAreaName, opt => opt.MapFrom(src => src.DestinationArea.Name))
              .ForMember(dest => dest.DestinationAreaId, opt => opt.MapFrom(src => src.DestinationAreaId))
              .ForMember(dest => dest.DriverName, opt => opt.MapFrom(src => src.Driver.Name))
              .ForMember(dest => dest.DriverId, opt => opt.MapFrom(src => src.DriverId))
              .ForMember(dest => dest.VehicleNumber, opt => opt.MapFrom(src => src.Vehicle.VehicleNumber))
              .ForMember(dest => dest.VehicleId, opt => opt.MapFrom(src => src.VehicleId));



            // If  use other DTOs (e.g., DriverDto, VehicleDto), map them here as well.
            // CreateMap<DriverDto, Driver>();
            // CreateMap<VehicleDto, Vehicle>();
        }
    }
}
