using System;
using AutoMapper;
using MyApi.models.entities;
namespace MyApi.Mappings;

public class EmployeeProfile:Profile
{
    public EmployeeProfile()
    {
        CreateMap<Addemployeedto,Employee>();
        CreateMap<Updateemployeedto,Employee>();
        CreateMap<Employee,Reademployeedto>();
    }
}
