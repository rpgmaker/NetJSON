NetJSON
=======

Faster than Any Binary?

Build Status
============
Build is successful. Failure is due .net core not available for 1.6 in appveyor
![alt tag](https://ci.appveyor.com/api/projects/status/github/rpgmaker/netjson?svg=true)

Quick Guide
===========
https://github.com/rpgmaker/NetJSON/wiki/Quick-Guide

Benchmark How Fast?
===================

http://theburningmonk.com/2014/08/json-serializers-benchmarks-updated-2/

3.4x Faster Than JSON.NET

2.4x Faster Than Service Stack JSON

1.8x Faster Than Jil

6x Faster Than JSON.NET BSON

16.5x Faster Than Microsoft JavaScriptSerializer

4.3x Faster Than DataContractJsonSerializer

1.6x Faster Than Protobuf-net
======================================================

- Without Outcome Filter of Min and Max
```
Test Group [Protobuf-Net], Test [Serialization] results summary:
Successes   [5]
Failures    [0]
```
- Average Exec Time [257.9771] milliseconds
```
Test Group [Protobuf-Net] average serialized byte array size is [51.72424]
Test Group [Protobuf-Net], Test [Deserialization] results summary:
Successes   [5]
Failures    [0]
Average Exec Time [251.4977] milliseconds
```


- Test Group [NetJson], Test [Serialization] results summary:
```
Successes   [5]
Failures    [0]
```
- Average Exec Time [157.98844] milliseconds
```
Test Group [NetJson] average serialized byte array size is [98.86456]
Test Group [NetJson], Test [Deserialization] results summary:
Successes   [5]
Failures    [0]
Average Exec Time [252.18208] milliseconds
```

How to Use
==========

```csharp
var myObject = new SimpleObject(){ ID = 100, Name = "Test", Value = "Value" };

var json = NetJSON.Serialize(myObject);

var recreatedObject = NetJSON.Deserialize<SimpleObject>(json);
```

Other Downloads
===============

Nuget: https://www.nuget.org/packages/NetJSON/
