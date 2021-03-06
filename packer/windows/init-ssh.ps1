# Licensed to the Apache Software Foundation (ASF) under one or more
# contributor license agreements.  See the NOTICE file distributed with
# this work for additional information regarding copyright ownership.
# The ASF licenses this file to You under the Apache License, Version 2.0
# (the "License"); you may not use this file except in compliance with
# the License.  You may obtain a copy of the License at
#
#      http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

$ssh = "C:\Users\Administrator\.ssh"
$authorized_keys = "$ssh\authorized_keys"
if ( -not (Test-Path $authorized_keys -PathType Leaf) ) {

  write-host "Installing SSH authorized key"

  mkdir -p $ssh -ErrorAction SilentlyContinue

  Invoke-WebRequest -Uri 'http://169.254.169.254/latest/meta-data/public-keys/0/openssh-key' -OutFile $authorized_keys

  # Give sshd permission to read authorized_keys
  Import-Module 'C:\Program Files\OpenSSH-Win64\OpenSSHUtils' -force

  $currentUserSid = Get-UserSID -User "NT SERVICE\sshd"
  $account = Get-UserAccount -UserSid $currentUserSid
  $ace = New-Object System.Security.AccessControl.FileSystemAccessRule `
                            ($account, "Read", "None", "None", "Allow")
  $acl = Get-Acl $authorized_keys
  $acl.AddAccessRule($ace)
  Enable-Privilege SeRestorePrivilege | out-null
  Set-Acl -Path $authorized_keys -AclObject $acl -Confirm:$false

}
