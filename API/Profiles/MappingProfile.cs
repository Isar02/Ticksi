using AutoMapper;
using API.Entities;
using API.DTOs;

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
            }
        }
   
}
