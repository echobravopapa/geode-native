/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#pragma once

#ifndef GEODE_DATASERIALIZABLE_H_
#define GEODE_DATASERIALIZABLE_H_

#include "internal/geode_globals.hpp"
#include "Serializable.hpp"

namespace apache {
namespace geode {
namespace client {

class DataOutput;
class DataInput;

/**
 * An interface for objects whose state can be written/read as primitive types.
 */
class APACHE_GEODE_EXPORT DataSerializable : public virtual Serializable {
 public:
  ~DataSerializable() override = default;

  /**
   * @brief serialize this object
   **/
  virtual void toData(DataOutput& dataOutput) const = 0;

  /**
   * @brief deserialize this object.
   **/
  virtual void fromData(DataInput& dataInput) = 0;

  /**
   * @brief Return the classId of the instance being serialized.
   * This is used by deserialization to determine what instance
   * type to create and deserialize into.
   *
   * The classId must be unique within an application suite.
   * Using a negative value may result in undefined behavior.
   */
  virtual int32_t getClassId() const = 0;
};

}  // namespace client
}  // namespace geode
}  // namespace apache

#endif  // GEODE_DATASERIALIZABLE_H_
