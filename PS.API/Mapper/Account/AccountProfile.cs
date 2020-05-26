using AutoMapper;
using PS.External.Model;
using PS.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PS.API.Mapper
{
    /// <summary>
    /// 账号相关映射
    /// </summary>
    public class AccountProfile : Profile
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public AccountProfile()
        {
            CreateMap<Login, RLoginDto>()
                .ForMember(dest=>dest.Name,opt=>
                    opt.MapFrom(src=>src.FirstName+src.LastName));
        }
    }
    /// <summary>
    /// 登录信息相关映射
    /// </summary>
    public class LoginProfile : Profile
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public LoginProfile()
        {
            CreateMap<Login, RLoginDto>()
                .ForMember(dest => dest.Name, opt =>
                       opt.MapFrom(src => src.FirstName + src.LastName));
        }
    }
}
