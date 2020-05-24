using AutoMapper;
using PS.External.Model;
using PS.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PS.API.Extension
{
    /// <summary>
    /// 账号相关映射
    /// </summary>
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            CreateMap<Login, RLoginDto>()
                .ForMember(dest=>dest.Name,opt=>
                    opt.MapFrom(src=>src.FirstName+src.LastName));
        }
    }
    public class LoginProfile : Profile
    {
        public LoginProfile()
        {
            CreateMap<Login, RLoginDto>()
                .ForMember(dest => dest.Name, opt =>
                       opt.MapFrom(src => src.FirstName + src.LastName));
        }
    }
}
