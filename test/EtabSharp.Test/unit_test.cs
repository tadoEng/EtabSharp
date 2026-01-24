using EtabSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtabSharp.Test;


public class UnitTest
{
    [Fact]
    public void TestConnection()
    {
        ETABSApplication modelApplication = ETABSWrapper.Connect();

        var model = modelApplication.Model;

        Assert.NotNull(model);

    }
}

