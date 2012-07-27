﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using THOK.Wms.Bll.Interfaces;
using THOK.Wms.DbModel;
using Microsoft.Practices.Unity;
using THOK.Wms.Dal.Interfaces;

namespace THOK.Wms.Bll.Service
{
    public class MoveBillMasterService:ServiceBase<MoveBillMaster>, IMoveBillMasterService
    {
        [Dependency]
        public IMoveBillMasterRepository MoveBillMasterRepository { get; set; }
        [Dependency]
        public IBillTypeRepository BillTypeRepository { get; set; }
        [Dependency]
        public IWarehouseRepository WarehouseRepository { get; set; }
        [Dependency]
        public IEmployeeRepository EmployeeRepository { get; set; }
        [Dependency]
        public IMoveBillDetailRepository MoveBillDetailRepository { get; set; }

        protected override Type LogPrefix
        {
            get { return this.GetType(); }
        }

        public string WhatStatus(string status)
        {
            string statusStr = "";
            switch (status)
            {
                case "1":
                    statusStr = "已录入";
                    break;
                case "2":
                    statusStr = "已审核";
                    break;
                case "3":
                    statusStr = "执行中";
                    break;
                case "4":
                    statusStr = "已移库";
                    break;
            }
            return statusStr;
        }

        #region IMoveBillMasterService 成员

        public object GetDetails(int page, int rows, string BillNo, string BillDate, string OperatePersonCode, string Status, string IsActive)
        {
            IQueryable<MoveBillMaster> moveBillMasterQuery = MoveBillMasterRepository.GetQueryable();
            var moveBillMaster = moveBillMasterQuery.Where(i => i.BillNo.Contains(BillNo)
                && i.Status != "4").OrderBy(i => i.BillNo).AsEnumerable().Select(i => new
                {
                    i.BillNo,
                    BillDate = i.BillDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    i.OperatePersonID,
                    i.WarehouseCode,
                    i.BillTypeCode,
                    i.BillType.BillTypeName,
                    i.Warehouse.WarehouseName,
                    OperatePersonCode = i.OperatePerson.EmployeeCode,
                    OperatePersonName = i.OperatePerson.EmployeeName,
                    VerifyPersonID = i.VerifyPersonID == null ? string.Empty : i.VerifyPerson.EmployeeCode,
                    VerifyPersonName = i.VerifyPersonID == null ? string.Empty : i.VerifyPerson.EmployeeName,
                    VerifyDate = (i.VerifyDate == null ? "" : ((DateTime)i.VerifyDate).ToString("yyyy-MM-dd HH:mm:ss")),
                    Status = WhatStatus(i.Status),
                    IsActive = i.IsActive == "1" ? "可用" : "不可用",
                    Description = i.Description,
                    UpdateTime = i.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
                });
            if (!IsActive.Equals(""))
            {
                moveBillMaster = moveBillMaster.Where(i =>
                    i.BillNo.Contains(BillNo)
                    && i.IsActive.Contains(IsActive)
                    && i.Status != "4").OrderBy(i => i.BillNo).AsEnumerable().Select(i => new
                    {
                        i.BillNo,
                        i.BillDate,
                        i.OperatePersonID,
                        i.WarehouseCode,
                        i.BillTypeCode,
                        i.BillTypeName,
                        i.WarehouseName,
                        i.OperatePersonCode,
                        i.OperatePersonName,
                        i.VerifyPersonID,
                        i.VerifyPersonName,
                        i.VerifyDate,
                        Status = WhatStatus(i.Status),
                        IsActive = i.IsActive == "1" ? "可用" : "不可用",
                        Description = i.Description,
                        UpdateTime = i.UpdateTime
                    });
            }
            int total = moveBillMaster.Count();
            moveBillMaster = moveBillMaster.Skip((page - 1) * rows).Take(rows);
            return new { total, rows = moveBillMaster.ToArray() };
        }

        public bool Add(MoveBillMaster moveBillMaster, string userName)
        {
            bool result = false;
            var mbm = new MoveBillMaster();
            var employee = EmployeeRepository.GetQueryable().FirstOrDefault(i => i.UserName == userName);
            if (employee != null)
            {
                mbm.BillNo = moveBillMaster.BillNo;
                mbm.BillDate = moveBillMaster.BillDate;
                mbm.BillTypeCode = moveBillMaster.BillTypeCode;
                mbm.WarehouseCode = moveBillMaster.WarehouseCode;
                mbm.OperatePersonID = employee.ID;
                mbm.Status = "1";
                mbm.VerifyPersonID = moveBillMaster.VerifyPersonID;
                mbm.VerifyDate = moveBillMaster.VerifyDate;
                mbm.Description = moveBillMaster.Description;
                mbm.IsActive = moveBillMaster.IsActive;
                mbm.UpdateTime = DateTime.Now;

                MoveBillMasterRepository.Add(mbm);
                MoveBillMasterRepository.SaveChanges();
                result = true;
            }
            return result;
        }

        public bool Delete(string BillNo)
        {
            bool result = false;
            var mbm = MoveBillMasterRepository.GetQueryable().FirstOrDefault(i => i.BillNo == BillNo && i.Status == "1");
            if (mbm != null)
            {
                Del(MoveBillDetailRepository, mbm.MoveBillDetails);
                MoveBillMasterRepository.Delete(mbm);
                MoveBillMasterRepository.SaveChanges();
                result = true;
            }
            return result;
        }

        public bool Save(MoveBillMaster moveBillMaster)
        {
            bool result = false;
            var mbm = MoveBillMasterRepository.GetQueryable().FirstOrDefault(i => i.BillNo == moveBillMaster.BillNo && i.Status == "1");
            if (mbm != null)
            {
                mbm.BillDate = moveBillMaster.BillDate;
                mbm.BillTypeCode = moveBillMaster.BillTypeCode;
                mbm.WarehouseCode = moveBillMaster.WarehouseCode;
                mbm.OperatePersonID = moveBillMaster.OperatePersonID;
                mbm.Status = "1";
                mbm.VerifyPersonID = moveBillMaster.VerifyPersonID;
                mbm.VerifyDate = moveBillMaster.VerifyDate;
                mbm.Description = moveBillMaster.Description;
                mbm.IsActive = moveBillMaster.IsActive;
                mbm.UpdateTime = DateTime.Now;

                MoveBillMasterRepository.SaveChanges();
                result = true;
            }
            return result;
        }

        public object GenMoveBillNo(string userName)
        {
            IQueryable<MoveBillMaster> moveBillMasterQuery = MoveBillMasterRepository.GetQueryable();
            string sysTime = System.DateTime.Now.ToString("yyMMdd");
            string billNo = "";
            var employee = EmployeeRepository.GetQueryable().FirstOrDefault(i => i.UserName == userName);
            var inBillMaster = moveBillMasterQuery.Where(i => i.BillNo.Contains(sysTime)).AsEnumerable().OrderBy(i => i.BillNo).Select(i => new { i.BillNo }.BillNo);
            if (inBillMaster.Count() == 0)
            {
                billNo = System.DateTime.Now.ToString("yyMMdd") + "0001" + "MO";
            }
            else
            {
                string billNoStr = inBillMaster.Last(b => b.Contains(sysTime));
                int i = Convert.ToInt32(billNoStr.ToString().Substring(6, 4));
                i++;
                string newcode = i.ToString();
                for (int j = 0; j < 4 - i.ToString().Length; j++)
                {
                    newcode = "0" + newcode;
                }
                billNo = System.DateTime.Now.ToString("yyMMdd") + newcode + "MO";
            }
            var findBillInfo = new
            {
                BillNo = billNo,
                billNoDate = DateTime.Now.ToString("yyyy-MM-dd"),
                employeeID = employee == null ? "" : employee.ID.ToString(),
                employeeCode = employee == null ? "" : employee.EmployeeCode.ToString(),
                employeeName = employee == null ? "" : employee.EmployeeName.ToString()
            };
            return findBillInfo;
        }

        public bool Audit(string BillNo, string userName)
        {
            bool result = false;
            var mbm = MoveBillMasterRepository.GetQueryable().FirstOrDefault(i => i.BillNo == BillNo && i.Status == "1");
            var employee = EmployeeRepository.GetQueryable().FirstOrDefault(i => i.UserName == userName);
            if (mbm != null)
            {
                mbm.Status = "2";
                mbm.VerifyDate = DateTime.Now;
                mbm.UpdateTime = DateTime.Now;
                mbm.VerifyPersonID = employee.ID;
                MoveBillMasterRepository.SaveChanges();
                result = true;
            }
            return result;
        }

        public bool AntiTrial(string BillNo)
        {
            bool result = false;
            var mbm = MoveBillMasterRepository.GetQueryable().FirstOrDefault(i => i.BillNo == BillNo && i.Status == "2");
            if (mbm != null)
            {
                mbm.Status = "1";
                mbm.VerifyDate = null;
                mbm.UpdateTime = DateTime.Now;
                mbm.VerifyPersonID = null;
                MoveBillMasterRepository.SaveChanges();
                result = true;
            }
            return result;
        }

        public object GetBillTypeDetail(string BillClass, string IsActive)
        {
            throw new NotImplementedException();
        }

        public object GetWareHouseDetail(string IsActive)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}