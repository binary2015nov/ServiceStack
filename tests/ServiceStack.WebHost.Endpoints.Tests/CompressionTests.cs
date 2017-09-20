using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
	public class TestCompress
	{
		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string Name { get; set; }

		public TestCompress()
		{
		}

		public TestCompress(int id, string name)
		{
			Id = id;
			Name = name;
		}
	}


	[TestFixture]
	public class CompressionTests
	{
		[Test]
		public void Can_compress_and_decompress_SimpleDto()
		{
			var simpleDto = new TestCompress(1, "name");

			var simpleDtoXml = ServiceStack.Serialization.DataContractSerializer.Instance.SerializeToString(simpleDto);

			var simpleDtoZip = simpleDtoXml.Deflate();

			Assert.That(simpleDtoZip.Length, Is.GreaterThan(0));

			var deserializedSimpleDtoXml = simpleDtoZip.Inflate();

			Assert.That(deserializedSimpleDtoXml, Is.Not.Empty);

			var deserializedSimpleDto = ServiceStack.Serialization.DataContractSerializer.Instance.DeserializeFromString<TestCompress>(
				deserializedSimpleDtoXml);

			Assert.That(deserializedSimpleDto, Is.Not.Null);

			Assert.That(deserializedSimpleDto.Id, Is.EqualTo(simpleDto.Id));
			Assert.That(deserializedSimpleDto.Name, Is.EqualTo(simpleDto.Name));
		}


		[Test]
		public void Test_response_with_CompressedResult()
		{
			using (new BasicAppHost(typeof(CompressionTests).GetAssembly()).Init())
			{
				var mockResponse = new MockHttpResponse();

				var simpleDto = new TestCompress(1, "name");

				var simpleDtoXml = ServiceStack.Serialization.DataContractSerializer.Instance.SerializeToString(simpleDto);

				const string expectedXml = "<TestCompress xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.ddnglobal.com/types/\"><Id>1</Id><Name>name</Name></TestCompress>";
				const string expectedXmlNetCore = "<TestCompress xmlns=\"http://schemas.ddnglobal.com/types/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Id>1</Id><Name>name</Name></TestCompress>";

				Assert.That(simpleDtoXml, Is.EqualTo(expectedXml).Or.EqualTo(expectedXmlNetCore));

				var simpleDtoZip = simpleDtoXml.Deflate();

				Assert.That(simpleDtoZip.Length, Is.GreaterThan(0));

				var compressedResult = new CompressedResult(simpleDtoZip);

				var reponseWasAutoHandled = mockResponse.WriteToResponse(
					compressedResult, CompressionTypes.Deflate);

				Assert.That(reponseWasAutoHandled.Result, Is.True);

				//var bytesToWriteToResponseStream = new byte[simpleDtoZip.Length - 4];
				//Array.Copy(simpleDtoZip, CompressedResult.Adler32ChecksumLength, bytesToWriteToResponseStream, 0, bytesToWriteToResponseStream.Length);

				var bytesToWriteToResponseStream = simpleDtoZip;

				var writtenBytes = mockResponse.ReadAsBytes();
				Assert.That(writtenBytes, Is.EqualTo(bytesToWriteToResponseStream));
				Assert.That(mockResponse.ContentType, Is.EqualTo(MimeTypes.Xml));
				Assert.That(mockResponse.Headers[HttpHeaders.ContentEncoding], Is.EqualTo(CompressionTypes.Deflate));

				Console.WriteLine("Content-length: " + writtenBytes.Length);
				Console.WriteLine(BitConverter.ToString(writtenBytes));
			}
		}

		[Test]
		public void Can_gzip_and_gunzip_SimpleDto()
		{
			var simpleDto = new TestCompress(1, "name");

			var simpleDtoXml = ServiceStack.Serialization.DataContractSerializer.Instance.SerializeToString(simpleDto);

			var simpleDtoZip = simpleDtoXml.GZip();

			Assert.That(simpleDtoZip.Length, Is.GreaterThan(0));

			var deserializedSimpleDtoXml = simpleDtoZip.GUnzip();

			Assert.That(deserializedSimpleDtoXml, Is.Not.Empty);

			var deserializedSimpleDto = ServiceStack.Serialization.DataContractSerializer.Instance.DeserializeFromString<TestCompress>(
				deserializedSimpleDtoXml);

			Assert.That(deserializedSimpleDto, Is.Not.Null);

			Assert.That(deserializedSimpleDto.Id, Is.EqualTo(simpleDto.Id));
			Assert.That(deserializedSimpleDto.Name, Is.EqualTo(simpleDto.Name));
		}

	}
}