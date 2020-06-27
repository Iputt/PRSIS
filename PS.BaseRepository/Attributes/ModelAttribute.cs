using System;
using System.Collections.Generic;
using System.Text;

namespace PS.BaseRepository.Attributes
{
    /// <summary>
    /// model的实体特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class,AllowMultiple =false)]
    public class ModelAttribute : Attribute
    {

    }
}
