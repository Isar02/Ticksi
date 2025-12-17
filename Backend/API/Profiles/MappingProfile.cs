using AutoMapper;
using Ticksi.Domain.Entities;
using Ticksi.Application.DTOs;

namespace API.Profiles
{
   
        public class MappingProfile : Profile
        {
            public MappingProfile()
            {
                // EventCategory Mappings
                CreateMap<EventCategory, EventCategoryReadDto>();
                CreateMap<EventCategoryCreateDto, EventCategory>();
                CreateMap<EventCategoryUpdateDto, EventCategory>();

                // Event Mappings
                CreateMap<Event, EventReadDto>()
                    .ForMember(dest => dest.EventCategoryName, opt => opt.MapFrom(src => src.EventCategory != null ? src.EventCategory.Name : string.Empty))
                    .ForMember(dest => dest.EventCategoryPublicId, opt => opt.MapFrom(src => src.EventCategory != null ? src.EventCategory.PublicId : Guid.Empty))
                    .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : string.Empty))
                    .ForMember(dest => dest.EventTypeName, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.Name : string.Empty))
                    .ForMember(dest => dest.OrganizerCompanyName, opt => opt.MapFrom(src => src.OrganizerCompany != null ? src.OrganizerCompany.Name : string.Empty));
            }
        }
   
}
