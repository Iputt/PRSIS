using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
namespace Owl.Feature.iHR
{

    /// <summary>
    /// 组织结构
    /// </summary>
    public class Organization : SmartObject
    {
        public static string JianRen = Owl.Feature.Translation.Get("text.owl.feature.hr.workpost.jianren", "兼", true);

        /// <summary>
        /// 组织结构代码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 展示层数
        /// </summary>
        public int? DisplayLayer { get; set; }

        #region 公司
        Dictionary<string, Company> m_companies;
        protected Dictionary<string, Company> Companies
        {
            get
            {
                if (m_companies == null)
                    m_companies = new Dictionary<string, Company>();
                return m_companies;
            }
        }
        public void AddCompany(Company company)
        {
            Companies[company.Code] = company;
        }
        #endregion

        #region 部门
        Dictionary<string, Department> m_departments;
        /// <summary>
        /// 部门
        /// </summary>
        protected Dictionary<string, Department> Departments
        {
            get
            {
                if (m_departments == null)
                    m_departments = new Dictionary<string, Department>();
                return m_departments;
            }
        }
        /// <summary>
        /// 添加部门到组织结构
        /// </summary>
        /// <param name="department"></param>
        public void AddDepartment(Department department)
        {
            Departments[string.Format("{0}___{1}", department.BUKRS, department.Code)] = department;
        }
        /// <summary>
        /// 获取所有部门
        /// </summary>
        /// <param name="companyasdepartment">是否将公司作为部门</param>
        /// <returns></returns>
        public IEnumerable<Department> GetDepartments(bool companyasdepartment = false)
        {
            if (!companyasdepartment)
                return Departments.Values;

            List<Department> departments = new List<Department>(Departments.Values);
            var rootcode = Code + Util.Serial.GetRandom(5, false);
            var rootdepartment = new Department(this) { Code = rootcode, Name = Name, ShowInOrg = true };
            foreach (var company in Companies.Values)
            {
                var topdepart = departments.FirstOrDefault(s => s.BUKRS == company.Code && string.IsNullOrEmpty(s.TopCode));
                if (topdepart != null)
                {
                    var crdepart = new Department(this) { Code = company.Code, Name = company.Name, BUKRS = company.Code, TopCode = rootcode, ShowInOrg = true, Parent = rootdepartment };
                    topdepart.TopCode = company.Code;
                    AddDepartment(crdepart);
                    departments.Add(crdepart);
                }
            }
            AddDepartment(rootdepartment);
            departments.Add(rootdepartment);
            return departments;

        }
        /// <summary>
        /// 从组织结构中获取部门
        /// </summary>
        /// <param name="bukrs">公司代码</param>
        /// <param name="code">部门代码</param>
        /// <returns></returns>
        public Department GetDepartment(string bukrs, string code)
        {
            var key = string.Format("{0}___{1}", bukrs, code);
            if (Departments.ContainsKey(key))
                return Departments[key];
            return null;
        }
        /// <summary>
        /// 获取子部门
        /// </summary>
        /// <param name="bukrs">公司代码</param>
        /// <param name="code">部门代码</param>
        /// <returns></returns>
        public IEnumerable<Department> GetChildDepartments(string bukrs, string code)
        {
            if (string.IsNullOrEmpty(bukrs))
                return Departments.Values.Where(s => s.TopCode == code);
            return Departments.Values.Where(s => s.BUKRS == bukrs && s.TopCode == code);
        }
        #endregion

        #region 岗位
        Dictionary<string, Workpost> m_workposts;
        /// <summary>
        /// 岗位列表
        /// </summary>
        protected Dictionary<string, Workpost> Workposts
        {
            get
            {
                if (m_workposts == null)
                    m_workposts = new Dictionary<string, Workpost>();
                return m_workposts;
            }
        }
        /// <summary>
        /// 将岗位添加进组织结构中
        /// </summary>
        /// <param name="workpost"></param>
        public void AddWorkpost(Workpost workpost)
        {
            if (workpost != null)
            {
                Workposts[string.Format("{0}__{1}", workpost.BUKRS, workpost.Code)] = workpost;
                //workpost.Department.AddWorkpost(workpost);
            }
        }
        /// <summary>
        /// 获取岗位
        /// </summary>
        /// <param name="bukrs"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public Workpost GetWorkpost(string bukrs, string code)
        {
            var key = string.Format("{0}__{1}", bukrs, code);
            return Workposts.ContainsKey(key) ? Workposts[key] : null;
        }

        public IEnumerable<Workpost> GetWorkposts(Department department)
        {
            return Workposts.Values.Where(s => s.Department == department);
        }
        #endregion

        #region 员工
        Dictionary<string, Staff> m_staff;
        /// <summary>
        /// 员工
        /// </summary>
        public Dictionary<string, Staff> Staffs
        {
            get
            {
                if (m_staff == null)
                    m_staff = new Dictionary<string, Staff>();
                return m_staff;
            }
        }

        public void AddStaff(Staff staff)
        {
            Staffs[staff.Code] = staff;
        }
        #endregion

        #region 组织关系
        Dictionary<string, List<Ship>> m_wpships;
        /// <summary>
        /// 岗位关系
        /// </summary>
        public Dictionary<string, List<Ship>> WpShips
        {
            get
            {
                if (m_wpships == null)
                    m_wpships = new Dictionary<string, List<Ship>>();
                return m_wpships;
            }
        }
        Dictionary<string, List<Ship>> m_staffships;
        /// <summary>
        /// 员工关系
        /// </summary>
        public Dictionary<string, List<Ship>> StaffShips
        {
            get
            {
                if (m_staffships == null)
                    m_staffships = new Dictionary<string, List<Ship>>();
                return m_staffships;
            }
        }

        public void AddShip(Ship ship)
        {
            var wpkey = string.Format("{0}{1}", ship.BUKRS, ship.WorkpostCode);
            if (!WpShips.ContainsKey(wpkey))
            {
                WpShips[wpkey] = new List<Ship>();
            }
            WpShips[wpkey].Add(ship);
            if (!StaffShips.ContainsKey(ship.StaffCode))
            {
                StaffShips[ship.StaffCode] = new List<Ship>();
            }
            StaffShips[ship.StaffCode].Add(ship);
        }
        #endregion

        #region 职位（待删除）
        //Dictionary<string, Tuple<string, string>> dpcodes;
        ///// <summary>
        ///// 获取岗位
        ///// </summary>
        ///// <param name="dpcode">部门和岗位组合代码</param>
        ///// <returns></returns>
        //public Tuple<string, string> GetPosition(string dpcode)
        //{
        //    if (dpcodes == null)
        //        dpcodes = Departments.Values.SelectMany(s => s.Positions.Values).ToDictionary(s => string.Format("{0}_{1}", s.Department.Code, s.Code), s => new Tuple<string, string>(s.Department.Code, s.Code));
        //    if (dpcodes.ContainsKey(dpcode))
        //        return dpcodes[dpcode];
        //    return null;
        //}
        //public Position GetPosition(string bukrs, string departmentcode, string positioncode)
        //{
        //    var department = GetDepartment(bukrs, departmentcode);
        //    if (department != null)
        //    {
        //        return department.GetPosition(positioncode);
        //    }
        //    return null;
        //}
        ///// <summary>
        ///// 获取职务的等级
        ///// </summary>
        ///// <param name="bukrs"></param>
        ///// <param name="positioncode"></param>
        ///// <returns></returns>
        //public int? GetPositionLevel(string bukrs, string positioncode)
        //{
        //    int? plevel = null;
        //    foreach (var depart in Departments.Values)
        //    {
        //        if (depart.BUKRS == bukrs)
        //        {
        //            var tp = depart.Positions.Values.FirstOrDefault(s => s.Code == positioncode);
        //            if (tp != null)
        //            {
        //                plevel = tp.Level;
        //                break;
        //            }
        //        }
        //    }
        //    return plevel;
        //}
        #endregion

        /// <summary>
        /// 沿着树状结构获取指定职位的员工
        /// </summary>
        /// <param name="bukrs">公司代码</param>
        /// <param name="departmentcode">部门代码</param>
        /// <param name="positioncode">职位代码</param>
        /// <returns></returns>
        public IEnumerable<Staff> GetStaffs(string bukrs, string departmentcode, string positioncode)
        {
            var department = GetDepartment(bukrs, departmentcode);
            if (department != null && !string.IsNullOrEmpty(positioncode))
                return department.GetWorkpostsWithPosition(positioncode).SelectMany(s => s.Staffs).Distinct();
            return new List<Staff>();
            //if (string.IsNullOrEmpty(positioncode))
            //    return new List<Staff>();
            //var position = GetPosition(bukrs, departmentcode, positioncode);
            //return position == null ? null : position.Staffs;
        }
        /// <summary>
        /// 获取领导
        /// </summary>
        /// <param name="bukrs"></param>
        /// <param name="departmentcode"></param>
        /// <param name="staffcode"></param>
        /// <returns></returns>
        public IEnumerable<Staff> GetLeaders(string bukrs, string departmentcode, string staffcode)
        {
            if (Staffs.ContainsKey(staffcode))
            {
                var workpost = Staffs[staffcode].GetHighest(bukrs, departmentcode);
                if (workpost != null)
                {
                    var topworkpost = workpost.TopWorkpost;
                    if (topworkpost.Count() > 0)
                        return topworkpost.SelectMany(s => s.Staffs);
                }
            }
            return new List<Staff>();
            //staff.Ships.Where(s => s.BUKRS == bukrs && s.DepartmentCode == departmentcode);
            //return Staffs[staffcode].GetLeader(bukrs, departmentcode);
        }
        Dictionary<int, int> m_MaxStaffLevel = new Dictionary<int, int>();
        /// <summary>
        /// 获取指定层级的部门最大员工数
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public int GetMaxStaff(int level)
        {
            int maxcount = 0;
            if (!m_MaxStaffLevel.ContainsKey(level))
            {
                maxcount = Departments.Values.Where(s => s.GetLevel() == level).Max(s => s.DisplayPosts.Sum(t => t.Staffs.Count(k => k.ShowInOrg)) + 1);
                m_MaxStaffLevel[level] = maxcount;
            }
            else
                maxcount = m_MaxStaffLevel[level];
            return maxcount;
        }
    }
    /// <summary>
    /// 公司
    /// </summary>
    public class Company : SmartObject
    {
        public Company(Organization organization)
        {
            Organization = organization;
        }

        /// <summary>
        /// 组织结构
        /// </summary>
        public Organization Organization { get; private set; }

        /// <summary>
        /// 代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 部门
    /// </summary>
    public class Department : SmartObject
    {
        public Department(Organization organization)
        {
            Organization = organization;
        }

        /// <summary>
        /// 公司代码
        /// </summary>
        public string BUKRS { get; set; }
        /// <summary>
        /// 部门代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 上级部门
        /// </summary>
        public string TopCode { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? Start { get; set; }
        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// 显示在组织结构中
        /// </summary>
        public bool ShowInOrg { get; set; }

        /// <summary>
        /// 组织结构
        /// </summary>
        public Organization Organization { get; private set; }

        /// <summary>
        /// 级别
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 获取循环的部门
        /// </summary>
        /// <param name="department"></param>
        /// <returns></returns>
        protected string GetLoop(Department department)
        {
            var parent = _GetParent();
            if (parent == department)
                return Code;
            if (parent == null)
                return "";
            return parent.GetLoop(department);
        }
        string loopdepart;
        /// <summary>
        /// 循环部门
        /// </summary>
        public string LoopDepart
        {
            get
            {
                if (loopdepart == null)
                    loopdepart = GetLoop(this);
                return loopdepart;
            }
        }

        Department m_parent;
        protected Department _GetParent()
        {
            if (m_parent == null && !string.IsNullOrEmpty(TopCode))
                m_parent = Organization.GetDepartment(BUKRS, TopCode);
            return m_parent;
        }
        /// <summary>
        /// 上级部门
        /// </summary>
        public Department Parent
        {
            get
            {
                if (!string.IsNullOrEmpty(LoopDepart))
                {
                    throw new AlertException("owl.feature.hr.department.loop", "组织结构部门 {0}-{1} 之间存在循环！", Code, LoopDepart);
                }
                return _GetParent();
            }
            set
            {
                m_parent = value;
            }
        }
        /// <summary>
        /// 根部门，顶级部门
        /// </summary>
        public Department Root
        {
            get
            {
                if (Parent == null)
                    return this;
                return Parent.Root;
            }
        }

        IEnumerable<Department> m_children;
        /// <summary>
        /// 下级部门
        /// </summary>
        public IEnumerable<Department> Children
        {
            get
            {
                if (m_children == null)
                {
                    m_children = Organization.GetChildDepartments(BUKRS, Code);
                }
                return m_children;
            }
        }

        IEnumerable<Department> m_displaychildren;
        /// <summary>
        /// 可在组织结构中展示的下级部门
        /// </summary>
        public IEnumerable<Department> DisplayChildren
        {
            get
            {
                if (m_displaychildren == null)
                {
                    if (Organization.DisplayLayer.HasValue && GetLevel() >= Organization.DisplayLayer)
                        m_displaychildren = new Department[0];
                    else
                        m_displaychildren = Organization.GetChildDepartments(BUKRS, Code).Where(s => s.ShowInOrg);
                }
                return m_displaychildren;
            }
        }

        //Dictionary<string, Position> m_positions;
        ///// <summary>
        ///// 职位
        ///// </summary>
        //public Dictionary<string, Position> Positions
        //{
        //    get
        //    {
        //        if (m_positions == null)
        //            m_positions = new Dictionary<string, Position>();
        //        return m_positions;
        //    }
        //}
        /// <summary>
        /// 可在组织结构中展示的岗位
        /// </summary>
        public IEnumerable<Workpost> DisplayPosts
        {
            get
            {
                if (Organization.DisplayLayer.HasValue && GetLevel() >= Organization.DisplayLayer)
                {
                    var workposts = new List<Workpost>(Workposts);
                    foreach (var child in Children)
                    {
                        workposts.AddRange(child.DisplayPosts);
                    }
                    return workposts;
                }
                else
                    return Workposts;
            }
        }

        IEnumerable<Workpost> m_workposts;
        /// <summary>
        /// 本部门的岗位
        /// </summary>
        public IEnumerable<Workpost> Workposts
        {
            get
            {
                if (m_workposts == null)
                {
                    m_workposts = Organization.GetWorkposts(this);
                }
                return m_workposts;
            }
        }
        /// <summary>
        /// 在本部门中 获取领导岗或汇报岗位,若汇报岗位不存在则返回本部门的领导岗位
        /// </summary>
        /// <param name="topcode"></param>
        /// <returns></returns>
        public IEnumerable<Workpost> GetLeader(string topcode = null)
        {
            var workposts = new List<Workpost>();
            if (!string.IsNullOrEmpty(topcode))
                workposts.Add(Workposts.FirstOrDefault(s => s.Code == topcode));
            if (workposts.Count == 0)
                workposts.AddRange(Workposts.Where(s => s.IsLeader));
            return workposts;
        }
        /// <summary>
        /// 沿树状结构根据职位获取岗位
        /// </summary>
        /// <param name="positioncode"></param>
        /// <returns></returns>
        public IEnumerable<Workpost> GetWorkpostsWithPosition(string positioncode)
        {
            var result = Workposts.Where(s => s.PositionCode == positioncode);
            if (result.Count() == 0 && Parent != null)
                return Parent.GetWorkpostsWithPosition(positioncode);
            return result;
        }

        ///// <summary>
        ///// 沿树状结构获取职位,若当前部门的最高职位高于限定的职位且当前部门不存在本职位，则返回空
        ///// </summary>
        ///// <param name="code"></param>
        ///// <param name="level"></param>
        ///// <returns></returns>
        //public Position GetPosition(string code, int? level = null)
        //{
        //    if (level == null)
        //        level = Organization.GetPositionLevel(BUKRS, code);
        //    if (level == null)
        //        return null;
        //    if (Positions.ContainsKey(code))
        //        return Positions[code];
        //    else if (Positions.Values.Min(s => s.Level) < level)
        //        return null;
        //    if (Parent != null)
        //        return Parent.GetPosition(code, level);
        //    return null;
        //}

        /// <summary>
        /// 获取部门的层级
        /// </summary>
        /// <returns></returns>
        public int GetLevel()
        {
            if (Parent == null)
                return 1;
            return Parent.GetLevel() + 1;
        }

        /// <summary>
        /// 是否可展现
        /// </summary>
        public bool CanDisplay
        {
            get
            {
                return ShowInOrg && (!Organization.DisplayLayer.HasValue || GetLevel() <= Organization.DisplayLayer.Value);
            }
        }

        /// <summary>
        /// 获取本部门所占的列数
        /// </summary>
        /// <returns></returns>
        public int GetDisplayColumns()
        {
            if (DisplayChildren.Count() == 0)
                return 1;
            return DisplayChildren.Sum(s => s.GetDisplayColumns());
        }
        /// <summary>
        /// 获取本级别的最多员工数
        /// </summary>
        /// <param name="depart"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public int GetMaxStaff()
        {
            var level = GetLevel();
            return Organization.GetMaxStaff(level);
        }

        ///// <summary>
        ///// 获取本部门中职位最低的员工
        ///// </summary>
        ///// <returns></returns>
        //public IEnumerable<Staff> GetLowest(int positionlevel)
        //{
        //    foreach (var position in Positions.Values.Where(s => s.Level < positionlevel).OrderByDescending(s => s.Level))
        //    {
        //        if (position.Staffs.Count() > 0)
        //            return position.Staffs;
        //    }
        //    if (Parent != null)
        //        return Parent.GetLowest(positionlevel);
        //    return new Staff[0];
        //}
    }

    #region 待删除
    ///// <summary>
    ///// 职位
    ///// </summary>
    //public class Position : SmartObject
    //{
    //    public Position(Department department)
    //    {
    //        Department = department;
    //        BUKRS = department.BUKRS;
    //    }
    //    /// <summary>
    //    /// 部门
    //    /// </summary>
    //    public Department Department { get; private set; }
    //    /// <summary>
    //    /// 公司代码
    //    /// </summary>
    //    public string BUKRS { get; set; }
    //    /// <summary>
    //    /// 职位代码
    //    /// </summary>
    //    public string Code { get; set; }
    //    /// <summary>
    //    /// 职位名称
    //    /// </summary>
    //    public string Name { get; set; }

    //    /// <summary>
    //    /// 岗位名称
    //    /// </summary>
    //    public string PostName { get; set; }
    //    /// <summary>
    //    /// 职位等级
    //    /// </summary>
    //    public int Level { get; set; }

    //    public IEnumerable<Staff> Staffs
    //    {
    //        get
    //        {
    //            var structure = Department.Organization;
    //            var ships = structure.Ships.Values.Where(s => s.BUKRS == Department.BUKRS && s.DepartmentCode == Department.Code && s.PositionCode == Code && structure.Staffs.ContainsKey(s.StaffCode));
    //            return ships.Select(s => structure.Staffs[s.StaffCode]);
    //        }
    //    }
    //}
    #endregion

    /// <summary>
    /// 岗位
    /// </summary>
    public class Workpost : SmartObject
    {
        public Workpost(Department department)
        {
            Department = department;
        }

        /// <summary>
        /// 组织结构
        /// </summary>
        public Organization Organization { get { return Department.Organization; } }
        /// <summary>
        /// 部门
        /// </summary>
        public Department Department { get; private set; }

        /// <summary>
        /// 公司
        /// </summary>
        public string BUKRS { get { return Department.BUKRS; } }


        /// <summary>
        /// 代码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 领导岗位
        /// </summary>
        public bool IsLeader { get; set; }

        /// <summary>
        /// 汇报给目标岗位
        /// </summary>
        public string ReportTo { get; set; }

        /// <summary>
        /// 上级岗位
        /// </summary>
        public IEnumerable<Workpost> TopWorkpost
        {
            get
            {
                if (IsLeader)
                {
                    return Department.Parent == null ? new List<Workpost>() : Department.Parent.GetLeader(ReportTo);
                }
                else
                {
                    return Department.GetLeader(ReportTo);
                }
            }
        }


        /// <summary>
        /// 职位代码
        /// </summary>
        public string PositionCode { get; set; }

        int? level;
        /// <summary>
        /// 级别
        /// </summary>
        public int Level
        {
            get
            {
                if (level == null)
                {
                    if (IsLeader)
                        level = 1;
                    else if (string.IsNullOrEmpty(ReportTo))
                        level = 2;
                    else
                    {
                        var topwp = TopWorkpost.FirstOrDefault();
                        level = topwp == null ? 2 : (topwp.Level + 1);
                    }
                }
                return level.Value;
                ///return Department.Positions.ContainsKey(PositionCode) ? Department.Positions[PositionCode].Level : int.MaxValue;
            }
        }
        /// <summary>
        /// 岗位包含的员工
        /// </summary>
        public IEnumerable<Staff> Staffs
        {
            get
            {
                var wpkey = string.Format("{0}{1}", BUKRS, Code);
                if (Organization.WpShips.ContainsKey(wpkey))
                    return Organization.WpShips[wpkey].Select(s => s.Staff);
                return new Staff[0];
            }
        }
    }

    public class Staff : SmartObject
    {
        /// <summary>
        /// 组织结构
        /// </summary>
        public Organization Organization { get; private set; }

        public Staff(Organization organization)
        {
            Organization = organization;
        }

        /// <summary>
        /// 员工代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 员工名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 显示在组织结构中
        /// </summary>
        public bool ShowInOrg { get; set; }

        /// <summary>
        /// 员工关系
        /// </summary>
        public IEnumerable<Ship> Ships
        {
            get
            {
                return Organization.StaffShips.ContainsKey(Code) ? Organization.StaffShips[Code] : new List<Ship>();
            }
        }
        /// <summary>
        /// 获取本员工在某部门中的岗位
        /// </summary>
        /// <param name="department"></param>
        /// <returns></returns>
        public IEnumerable<Workpost> GetWorkposts(Department department = null)
        {
            if (Organization.StaffShips.ContainsKey(Code))
            {
                var workposts = Organization.StaffShips[Code].Select(s => s.Workpost);
                if (department != null)
                    return workposts.Where(s => s.Department == department);
                return workposts;
            }
            return new Workpost[0];
        }
        /// <summary>
        /// 获取指定部门中的最高岗位
        /// </summary>
        /// <param name="bukrs"></param>
        /// <param name="department"></param>
        /// <returns></returns>
        public Workpost GetHighest(string bukrs, string department)
        {
            Workpost result = null;
            if (Organization.StaffShips.ContainsKey(Code))
            {
                var workposts = Organization.StaffShips[Code].Where(s => s.BUKRS == bukrs && s.Workpost.Department.Code == department).Select(s => s.Workpost).ToList();
                if (workposts.Count == 1)
                    result = workposts[0];
                else
                {
                    result = workposts.FirstOrDefault(s => s.IsLeader);
                    if (result == null)
                        result = workposts.FirstOrDefault(s => string.IsNullOrEmpty(s.ReportTo));
                    if (result == null)
                        result = workposts.FirstOrDefault();
                }
            }
            return result;
        }

        public bool IsMainPost(Workpost post)
        {
            var mainship = Ships.FirstOrDefault(s => s.IsMain);
            if (mainship != null)
                return mainship.BUKRS == post.Department.BUKRS && mainship.WorkpostCode == post.Code;
            return false;
        }

        ///// <summary>
        ///// 获取员工的直属领导
        ///// </summary>
        ///// <param name="bukrs"></param>
        ///// <param name="department"></param>
        ///// <returns></returns>
        //public IEnumerable<Staff> GetLeader(string bukrs, string department)
        //{
        //    var ship = Ships.Where(s => s.BUKRS == bukrs && s.DepartmentCode == department && s.Position != null).OrderBy(s => s.Position.Level).FirstOrDefault();
        //    if (ship == null)
        //        return new List<Staff>();
        //    return ship.Department.GetLowest(ship.Position.Level);
        //}

    }
    /// <summary>
    /// 员工岗位关系
    /// </summary>
    public class Ship : SmartObject
    {
        public Organization Structure { get; private set; }

        public Ship(Organization structure)
        {
            Structure = structure;
        }

        /// <summary>
        /// 公司
        /// </summary>
        public string BUKRS { get; set; }


        /// <summary>
        /// 岗位代码
        /// </summary>
        public string WorkpostCode { get; set; }

        /// <summary>
        /// 员工代码
        /// </summary>
        public string StaffCode { get; set; }

        /// <summary>
        /// 是否主岗
        /// </summary>
        public bool IsMain { get; set; }

        #region 待删除
        ///// <summary>
        ///// 部门代码
        ///// </summary>
        //public string DepartmentCode { get; set; }

        ///// <summary>
        ///// 部门
        ///// </summary>
        //public Department Department
        //{
        //    get
        //    {
        //        return Structure.GetDepartment(BUKRS, DepartmentCode);
        //    }
        //}

        ///// <summary>
        ///// 职位代码
        ///// </summary>
        //public string PositionCode { get; set; }
        ///// <summary>
        ///// 职位
        ///// </summary>
        //public Position Position
        //{
        //    get
        //    {
        //        var department = Department;
        //        return department != null ? department.GetPosition(PositionCode) : null;
        //    }
        //}
        #endregion

        /// <summary>
        /// 岗位
        /// </summary>
        public Workpost Workpost
        {
            get { return Structure.GetWorkpost(BUKRS, WorkpostCode); }
        }

        public Staff Staff
        {
            get { return Structure.Staffs.ContainsKey(StaffCode) ? Structure.Staffs[StaffCode] : null; }
        }
    }
}
