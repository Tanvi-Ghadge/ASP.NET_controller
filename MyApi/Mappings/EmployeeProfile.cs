using System;
using AutoMapper;
using MyApi.DTO.Employee;
using MyApi.models.entities;
namespace MyApi.Mappings;

public class EmployeeProfile:Profile
{
    public EmployeeProfile()
    {
        CreateMap<Createemployeedto,Employee>();
        CreateMap<Updateemployeedto,Employee>();
        CreateMap<Employee,Reademployeedto>();
    }
}
