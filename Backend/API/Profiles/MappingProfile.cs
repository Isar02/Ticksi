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
            }
        }
   
}
