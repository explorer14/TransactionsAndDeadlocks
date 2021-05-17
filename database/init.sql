create database TrxDb;

create table TrxDb.Products
 (Id int not null primary key,
 Stock int not null,
 Version int not null);

insert into TrxDb.Products values(1000, 10, 1);
