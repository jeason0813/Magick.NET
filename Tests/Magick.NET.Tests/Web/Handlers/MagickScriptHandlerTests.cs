﻿//=================================================================================================
// Copyright 2013-2017 Dirk Lemstra <https://magick.codeplex.com/>
//
// Licensed under the ImageMagick License (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
//
//   http://www.imagemagick.org/script/license.php
//
// Unless required by applicable law or agreed to in writing, software distributed under the
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
// express or implied. See the License for the specific language governing permissions and
// limitations under the License.
//=================================================================================================

using ImageMagick;
using ImageMagick.Web;
using ImageMagick.Web.Handlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Magick.NET.Tests
{
  [TestClass]
  public class MagickScriptHandlerTests
  {
    private MagickFormatInfo JpgFormatInfo => MagickNET.GetFormatInformation(MagickFormat.Jpg);
    private Encoding Encoding = System.Text.Encoding.GetEncoding(1252);

    [TestMethod]
    public void Test_ProcessRequest()
    {
      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

      try
      {
        string config = $@"<magick.net.web cacheDirectory=""{tempDir}"" tempDirectory=""{tempDir}""/>";

        MagickWebSettings settings = TestSectionLoader.Load(config);

        TestUrlResolver resolver = new TestUrlResolver();
        resolver.FileName = Path.Combine(tempDir, "test.jpg");
        resolver.Format = MagickFormat.Png;
        resolver.Script = XElement.Load(Files.Scripts.Resize).CreateNavigator();

        File.Copy(Files.ImageMagickJPG, resolver.FileName);

        HttpRequest request = new HttpRequest("foo", "https://bar", "");

        string outputFile = Path.Combine(tempDir, "output.png");

        using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding))
        {
          HttpResponse response = new HttpResponse(writer);
          HttpContext context = new HttpContext(request, response);

          MagickScriptHandler handler = new MagickScriptHandler(settings, resolver, JpgFormatInfo);
          handler.ProcessRequest(context);
        }

        using (MagickImage image = new MagickImage(outputFile))
        {
          Assert.AreEqual(MagickFormat.Png, image.Format);
          Assert.AreEqual(62, image.Width);
          Assert.AreEqual(59, image.Height);
        }
        Assert.AreEqual(3, tempDir.GetFiles().Count());

        resolver.Format = MagickFormat.Tiff;

        outputFile = Path.Combine(tempDir, "output.tiff");

        using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding))
        {
          HttpResponse response = new HttpResponse(writer);
          HttpContext context = new HttpContext(request, response);

          MagickScriptHandler handler = new MagickScriptHandler(settings, resolver, JpgFormatInfo);
          handler.ProcessRequest(context);
        }

        using (MagickImage image = new MagickImage(outputFile))
        {
          Assert.AreEqual(MagickFormat.Tiff, image.Format);
          Assert.AreEqual(62, image.Width);
          Assert.AreEqual(59, image.Height);
        }
        Assert.AreEqual(5, tempDir.GetFiles().Count());

        File.Delete(outputFile);

        FileInfo cacheFile = tempDir.GetFiles().First();

        DateTime lastWriteTime = cacheFile.LastWriteTime;

        using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding))
        {
          HttpResponse response = new HttpResponse(writer);
          HttpContext context = new HttpContext(request, response);

          MagickScriptHandler handler = new MagickScriptHandler(settings, resolver, JpgFormatInfo);
          handler.ProcessRequest(context);
        }

        using (MagickImage image = new MagickImage(outputFile))
        {
          Assert.AreEqual(MagickFormat.Tiff, image.Format);
          Assert.AreEqual(62, image.Width);
          Assert.AreEqual(59, image.Height);
        }
        Assert.AreEqual(5, tempDir.GetFiles().Count());

        cacheFile.Refresh();
        Assert.AreEqual(lastWriteTime, cacheFile.LastWriteTime);

        Thread.Sleep(100);

        File.Delete(resolver.FileName);
        using (Stream input = File.OpenRead(Files.ImageMagickJPG))
        {
          using (Stream output = File.OpenWrite(resolver.FileName))
          {
            input.CopyTo(output);
          }
        }

        using (StreamWriter writer = new StreamWriter(outputFile))
        {
          HttpResponse response = new HttpResponse(writer);
          HttpContext context = new HttpContext(request, response);

          MagickScriptHandler handler = new MagickScriptHandler(settings, resolver, JpgFormatInfo);
          handler.ProcessRequest(context);
        }

        cacheFile.Refresh();

        Assert.AreNotEqual(lastWriteTime, cacheFile.LastWriteTime);
        Assert.AreEqual(5, tempDir.GetFiles().Count());
      }
      finally
      {
        if (Directory.Exists(tempDir))
          Directory.Delete(tempDir, true);
      }
    }
  }
}