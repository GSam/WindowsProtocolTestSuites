﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Protocols.TestSuites.FileSharing.Common.Adapter;
using Microsoft.Protocols.TestSuites.FileSharing.FSA.Adapter;
using Microsoft.Protocols.TestTools;
using Microsoft.Protocols.TestTools.StackSdk;
using Microsoft.Protocols.TestTools.StackSdk.FileAccessService.Fscc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestSuites.FileSharing.FSA.TestSuite
{
    public partial class AlternateDataStreamTestCases : PtfTestClassBase
    {
        #region Test Cases

        [TestMethod()]
        [TestCategory(TestCategories.Fsa)]
        [TestCategory(TestCategories.SetFileInformation)]
        [TestCategory(TestCategories.AlternateDataStream)]
        [Description("Set FileShortNameInformation to an Alternate Data Stream on a DataFile.")]
        public void AlternateDataStream_Set_FileShortNameInformation_File()
        {
            AlternateDataStream_Set_FileShortNameInformation(FileType.DataFile);
        }

        [TestMethod()]
        [TestCategory(TestCategories.Fsa)]
        [TestCategory(TestCategories.SetFileInformation)]
        [TestCategory(TestCategories.AlternateDataStream)]
        [Description("Set FileShortNameInformation to an Alternate Data Stream on a DirectoryFile.")]
        public void AlternateDataStream_Set_FileShortNameInformation_Dir()
        {
            AlternateDataStream_Set_FileShortNameInformation(FileType.DirectoryFile);
        }

        #endregion

        #region Test Case Utility

        private void AlternateDataStream_Set_FileShortNameInformation(FileType fileType)
        {
            BaseTestSite.Log.Add(LogEntryKind.TestStep, "Test case steps:");
            MessageStatus status = MessageStatus.SUCCESS;
            Dictionary<string, long> streamList = new Dictionary<string, long>();
            long bytesToWrite = 0;
            long bytesWritten = 0;

            //Step 1: Create a new File, it could be a DataFile or a DirectoryFile
            string fileName = this.fsaAdapter.ComposeRandomFileName(8);
            BaseTestSite.Log.Add(LogEntryKind.TestStep, "1. Create a file with type: " + fileType.ToString() + " and name: " + fileName);
            CreateOptions createFileType = (fileType == FileType.DataFile ? CreateOptions.NON_DIRECTORY_FILE : CreateOptions.DIRECTORY_FILE);
            status = this.fsaAdapter.CreateFile(
                        fileName,
                        FileAttribute.NORMAL,
                        createFileType,
                        FileAccess.GENERIC_ALL,
                        ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE | ShareAccess.FILE_SHARE_DELETE,
                        CreateDisposition.OPEN_IF);
            this.fsaAdapter.AssertIfNotSuccess(status, "Create file operation failed");

            //Step 2: Write some bytes into the Unnamed Data Stream in the newly created file
            if (fileType == FileType.DataFile)
            {
                //Write some bytes into the DataFile.
                bytesToWrite = 1024;
                bytesWritten = 0;
                streamList.Add("::$DATA", bytesToWrite);

                BaseTestSite.Log.Add(LogEntryKind.TestStep, "2. Write the file with " + bytesToWrite + " bytes data.");
                status = this.fsaAdapter.WriteFile(0, bytesToWrite, out bytesWritten);
                this.fsaAdapter.AssertIfNotSuccess(status, "Write data to file operation failed.");
            }
            else
            {
                //Do not write data into DirectoryFile.
                bytesToWrite = 0;
                BaseTestSite.Log.Add(LogEntryKind.TestStep, "2. Do not write data into DirectoryFile.");
            }

            //Step 3: Create an Alternate Data Stream <Stream1> in the newly created file
            string streamName1 = this.fsaAdapter.ComposeRandomFileName(8);

            BaseTestSite.Log.Add(LogEntryKind.TestStep, "3. Create an Alternate Data Stream with name: " + streamName1 + "on this file.");
            status = this.fsaAdapter.CreateFile(
                        fileName + ":" + streamName1 + ":$DATA",
                        FileAttribute.NORMAL,
                        CreateOptions.NON_DIRECTORY_FILE,
                        FileAccess.GENERIC_ALL,
                        ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE | ShareAccess.FILE_SHARE_DELETE,
                        CreateDisposition.OPEN_IF);
            this.fsaAdapter.AssertIfNotSuccess(status, "Create Alternate Data Stream operation failed");

            //Step 4: Write some bytes into the Alternate Data Stream <Stream1> in the file
            bytesToWrite = 2048;
            bytesWritten = 0;
            streamList.Add(":" + streamName1 + ":$DATA", bytesToWrite);

            BaseTestSite.Log.Add(LogEntryKind.TestStep, "4. Write the stream with " + bytesToWrite + " bytes data.");
            status = this.fsaAdapter.WriteFile(0, bytesToWrite, out bytesWritten);
            this.fsaAdapter.AssertIfNotSuccess(status, "Write data to stream operation failed.");

            //Step 5: Set FILE_SHORTNAME_INFORMATION
            FileShortNameInformation shortNameInfo = new FileShortNameInformation();

            string shortName = this.fsaAdapter.ComposeRandomFileName(8);
            shortNameInfo.FileName = Encoding.Unicode.GetBytes(shortName);
            shortNameInfo.FileNameLength = (uint)shortNameInfo.FileName.Length;

            byte[] inputBuffer = TypeMarshal.ToBytes<FileShortNameInformation>(shortNameInfo);

            BaseTestSite.Log.Add(LogEntryKind.TestStep, "5. SetFileInformation with FileInfoClass.FILE_SHORTNAME_INFORMATION.");
            status = this.fsaAdapter.SetFileInformation(FileInfoClass.FILE_SHORTNAME_INFORMATION, inputBuffer);
            if (this.fsaAdapter.IsShortNameSupported == false)
            {
                BaseTestSite.Log.Add(LogEntryKind.Debug, "FileShortNameInformation is not supported.");
                this.fsaAdapter.AssertAreEqual(this.Manager, MessageStatus.INVALID_PARAMETER, status,
                    "If a file system does not support a specific File Information Class, STATUS_INVALID_PARAMETER MUST be returned.");
            }
            else
            {
                if (status == MessageStatus.SHORT_NAMES_NOT_ENABLED_ON_VOLUME)
                {
                    BaseTestSite.Log.Add(LogEntryKind.Debug, "If Open.File.Volume.GenerateShortNames is FALSE, the operation MUST be failed with STATUS_SHORT_NAMES_NOT_ENABLED_ON_VOLUME.");
                    BaseTestSite.Log.Add(LogEntryKind.Debug, "To enable short name in specific volume, such as volume with driver letter N:, use command: fsutil 8dot3name set N: 0.");
                }
                else
                {
                    this.fsaAdapter.AssertAreEqual(this.Manager, MessageStatus.INVALID_PARAMETER, status,
                        "If Open.Stream.StreamType is DataStream and Open.Stream.Name is not empty. The operation MUST be failed with STATUS_INVALID_PARAMETER.");
                }
            }
        }

        #endregion

    }
}