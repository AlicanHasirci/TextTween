// namespace TextTween.Tests
// {
//     using NUnit.Framework;
//     using Unity.Collections;
//     using Unity.Mathematics;
//
//     [TestFixture]
//     public class TextDataManagerTests
//     {
//         private TextDataManager<MockTextData> _manager;
//
//         [SetUp]
//         public void SetUp()
//         {
//             _manager = new TextDataManager<MockTextData>();
//
//             _manager.Add(new MockTextData().Create(3));
//             _manager.Add(new MockTextData().Create(3));
//         }
//
//         [TearDown]
//         public void TearDown()
//         {
//             _manager.Dispose();
//         }
//
//         [Test]
//         public void Add()
//         {
//             _manager.Add(new MockTextData().Create(3));
//             AssertTextData();
//         }
//
//         [Test]
//         public void Remove()
//         {
//             ChangeTextData();
//             _manager.Remove(_manager.Texts[0]);
//             AssertTextData();
//         }
//
//         [Test]
//         public void Allocate()
//         {
//             _manager.Allocate();
//
//             AssertTextData();
//
//             _manager.Texts[0].Create(5);
//
//             _manager.Allocate();
//
//             AssertTextData();
//         }
//
//         [Test]
//         public void Revert()
//         {
//             _manager.Allocate();
//
//             ChangeTextData();
//
//             _manager.Revert();
//             AssertTextData();
//         }
//
//         [Test]
//         public void EnsureCapacity()
//         {
//             NativeArray<int> testArray = new(3, Allocator.Persistent);
//             testArray[0] = 0;
//             testArray[1] = 1;
//             testArray[2] = 2;
//             TextDataManager<MockTextData>.EnsureCapacity(ref testArray, 3);
//
//             Assert.AreEqual(3, testArray.Length);
//             Assert.AreEqual(0, testArray[0]);
//             Assert.AreEqual(1, testArray[1]);
//             Assert.AreEqual(2, testArray[2]);
//
//             TextDataManager<MockTextData>.EnsureCapacity(ref testArray, 10);
//             Assert.AreEqual(10, testArray.Length);
//             Assert.AreEqual(0, testArray[0]);
//             Assert.AreEqual(1, testArray[1]);
//             Assert.AreEqual(2, testArray[2]);
//
//             testArray.Dispose();
//         }
//
//         private void ChangeTextData()
//         {
//             foreach (MockTextData mock in _manager.Texts)
//             {
//                 for (int i = 0; i < mock.Vertices.Count; i++)
//                 {
//                     mock.Vertices[i] = -1 * mock.Vertices[i];
//                     mock.Colors[i] = -1 * mock.Colors[i];
//                 }
//             }
//         }
//
//         private void AssertTextData()
//         {
//             foreach (MockTextData mock in _manager.Texts)
//             {
//                 for (int i = 0; i < mock.DataLength; i++)
//                 {
//                     int index = mock.DataOffset + i;
//                     Assert.AreEqual((float3)i, _manager.Vertices[index]);
//                     Assert.AreEqual((float4)i, _manager.Colors[index]);
//                 }
//             }
//         }
//     }
// }
