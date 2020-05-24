using AutoMapper;
using PS.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PS.External.Model
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
}
