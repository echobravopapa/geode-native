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

cmake_minimum_required( VERSION 3.9 )
project( Apache.Geode.IntegrationTests2 CSharp )

set(CMAKE_CSharp_FLAGS "/langversion:5")

get_target_property(JAVAOBJECT_JAR_PATH javaobject JAR_FILE)

set(CRYPTO_IMPL_PATH "/../../../cryptoimpl/${CMAKE_BUILD_TYPE}")
set(OPENSSL_PATH "/../../../dependencies/openssl/openssl-extern-prefix/${CMAKE_BUILD_TYPE}/bin")

configure_file(${CMAKE_CURRENT_SOURCE_DIR}/packages.config ${CMAKE_CURRENT_BINARY_DIR}/packages.config COPYONLY)
configure_file(${CMAKE_CURRENT_SOURCE_DIR}/Config.cs.in ${CMAKE_CURRENT_BINARY_DIR}/Config.cs)

add_library( ${PROJECT_NAME} SHARED
    Config.cs.in
    ${CMAKE_CURRENT_BINARY_DIR}/Config.cs
    GeodeServer.cs
    GeodeServerTests.cs
    CacheXml.cs
    CacheXmlTests.cs
    RegionTest.cs
    RegionSSLTest.cs
    cache.xml
    geode.properties
    xunit.runner.json
    packages.config
    SerializationTests.cs
    ClientSslKeys/client_keystore.password.pem
    ClientSslKeys/client_truststore.pem
    ServerSslKeys/server_keystore.jks
    ServerSslKeys/server_truststore.jks
)

set_source_files_properties(cache.xml xunit.runner.json geode.properties ClientSslKeys/client_keystore.password.pem ClientSslKeys/client_truststore.pem ServerSslKeys/server_keystore.jks ServerSslKeys/server_truststore.jks PROPERTIES
  VS_COPY_TO_OUT_DIR Always
  VS_TOOL_OVERRIDE "None"
)

target_link_libraries(${PROJECT_NAME}
  PUBLIC
    Apache.Geode
    PdxClassLibrary
)

set_target_properties( ${PROJECT_NAME} PROPERTIES
  VS_GLOBAL_ROOTNAMESPACE ${PROJECT_NAME}
  VS_GLOBAL_TreatWarningsAsErrors True
  VS_GLOBAL_TestProjectType UnitTest
  VS_GLOBAL_PROJECT_TYPES "{3AC096D0-A1C2-E12C-1390-A8335801FDAB};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"
  VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.5.2"
  VS_DOTNET_REFERENCES "System;System.Xml;System.Web;System.Configuration"
  VS_DOTNET_REFERENCE_Microsoft.VisualStudio.TestPlatform.TestFramework "${CMAKE_BINARY_DIR}/clicache/packages/Microsoft.VisualStudio.TestPlatform.14.0.0.1/lib/net20/Microsoft.VisualStudio.TestPlatform.TestFramework.dll"
  VS_DOTNET_REFERENCE_Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions "${CMAKE_BINARY_DIR}/clicache/packages/Microsoft.VisualStudio.TestPlatform.14.0.0.1/lib/net20/Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions.dll"
  VS_DOTNET_REFERENCE_xunit.core "${CMAKE_BINARY_DIR}/clicache/packages/xunit.extensibility.core.2.3.1/lib/netstandard1.1/xunit.core.dll"
  VS_DOTNET_REFERENCE_xunit.assert "${CMAKE_BINARY_DIR}/clicache/packages/xunit.assert.2.3.1/lib/netstandard1.1/xunit.assert.dll"
  VS_DOTNET_REFERENCE_xunit.abstractions "${CMAKE_BINARY_DIR}/clicache/packages/xunit.abstractions.2.0.1/lib/net35/xunit.abstractions.dll"
  VS_DOTNET_REFERENCE_xunit.execution.desktop "${CMAKE_BINARY_DIR}/clicache/packages/xunit.extensibility.execution.2.3.1/lib/net452/xunit.execution.desktop.dll"
)

if(NOT "${STRONG_NAME_KEY}" STREQUAL "")
  set_target_properties( ${PROJECT_NAME} PROPERTIES
    VS_GLOBAL_SignAssembly "true"
    VS_GLOBAL_AssemblyOriginatorKeyFile ${STRONG_NAME_KEY}
  )
endif()

add_dependencies(${PROJECT_NAME} nuget-restore)
