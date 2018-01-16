// MongoDocument.cpp
// author: Johannes Wagner <wagner@hcm-lab.de>
// created: 2016/10/19
// Copyright (C) University of Augsburg, Lab for Human Centered Multimedia
//
// *************************************************************************************************
//
// This file is part of Social Signal Interpretation (SSI) developed at the 
// Lab for Human Centered Multimedia of the University of Augsburg
//
// This library is free software; you can redistribute itand/or
// modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or any laterversion.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FORA PARTICULAR PURPOSE.  See the GNU
// General Public License for more details.
//
// You should have received a copy of the GNU General Public
// License along withthis library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

#include "MongoDocument.h"
#include "BsonTools.h"
#include "MongoOID.h"
#include "MongoDate.h"

#include <bson.h>
#include <bcon.h>
#include <mongoc.h>

namespace ssi
{
	ssi_char_t *MongoDocument::ssi_log_name = "mongodoc__";

	MongoDocument::MongoDocument()
	{
		_document = bson_new();
		_borrowed = false;
	}

	MongoDocument::MongoDocument(const MongoDocument &document)
	{
		_document = bson_copy(document._document);
		_borrowed = false;
	}

	MongoDocument::MongoDocument(bson_t *borrow)
	{
		_document = borrow;
		_borrowed = true;
	}

	MongoDocument::MongoDocument(MongoOID *value)
	{
		bson_oid_t *oid = (bson_oid_t *)value->get();		
		_document = BCON_NEW(BsonTools::OID, BCON_OID(oid));
		_borrowed = false;
	}

	MongoDocument::MongoDocument(const ssi_char_t *key, MongoOID *value)
	{
		bson_oid_t *oid = (bson_oid_t *)value->get();
		_document = BCON_NEW(key, BCON_OID(oid));
		_borrowed = false;
	}

	MongoDocument::MongoDocument(const ssi_char_t *key, const ssi_char_t *value)
	{
		_document = BCON_NEW(key, BCON_UTF8(value));
		_borrowed = false;
	}

	MongoDocument::MongoDocument(const ssi_char_t *key, double value)
	{
		_document = BCON_NEW(key, BCON_DOUBLE(value));
		_borrowed = false;
	}

	MongoDocument::MongoDocument(const ssi_char_t *key, int32_t value)
	{
		_document = BCON_NEW(key, BCON_INT32(value));
		_borrowed = false;
	}

	MongoDocument::MongoDocument(const ssi_char_t *key, int64_t value)
	{
		_document = BCON_NEW(key, BCON_INT64(value));
	}

	MongoDocument::MongoDocument(const ssi_char_t *key, bool value)
	{
		_document = BCON_NEW(key, BCON_BOOL(value));
		_borrowed = false;
	}

	MongoDocument::MongoDocument(const ssi_char_t *key, MongoDate *value)
	{
		_document = BCON_NEW(key, BCON_DATE_TIME(value->getDate()));
		_borrowed = false;
	}

	MongoDocument::~MongoDocument()
	{
		if (_document && !_borrowed)
		{
			bson_destroy(_document);
		}
	}

	void MongoDocument::clear()
	{
		if (_document && !_borrowed)
		{
			bson_destroy(_document);
		}
		_document = bson_new();
		_borrowed = false;
	}

	bson_t *MongoDocument::get()
	{
		return _document;
	}

	MongoOID *MongoDocument::getOid()
	{
		bson_oid_t oid;
		if (BsonTools::Oid(_document, oid))
		{
			MongoOID *value = new MongoOID(oid.bytes);
			return value;
		}
		return 0;
	}

	MongoOID *MongoDocument::getOid(const ssi_char_t *key)
	{
		const bson_value_t *v = BsonTools::Value(_document, key);
		if (v)
		{
			if (v->value_type == BSON_TYPE_OID)
			{
				// we have to make a copy since the memory holding the bson oid will be overwritten
				// WHY????

				bson_oid_t copy;
				bson_oid_copy(&(v->value.v_oid), &copy);
				MongoOID *value = new MongoOID((void*)&copy);
				
				return value;
			}
		}
		return 0;
	}

	bool MongoDocument::setOid(MongoOID *value)
	{		
		return setOid(BsonTools::OID, value);
	}

	bool MongoDocument::setOid(const ssi_char_t *key, MongoOID *value)
	{
		bson_oid_t *oid = (bson_oid_t *)value->get();
		return bson_append_oid(_document, key, -1, oid);
	}

	ssi_char_t *MongoDocument::getString(const ssi_char_t *key, bool convertToUtf8)
	{
		const bson_value_t *v = BsonTools::Value(_document, key);
		if (v)
		{
			if (v->value_type == BSON_TYPE_UTF8)
			{
				if (convertToUtf8)
				{
					const ssi_char_t *value = v->value.v_utf8.str;
					return UTF8ToUnicode(value);
				}
				else
				{
					ssi_char_t *value = ssi_strcpy(v->value.v_utf8.str);
					return value;
				}					
			}
		}

		return 0;
	}

	bool MongoDocument::setString(const ssi_char_t *key, const ssi_char_t *value, bool convertToUtf8)
	{
		if (convertToUtf8)
		{
			char *utf8 = UnicodeToUTF8(value);
			bool result = bson_append_utf8(_document, key, -1, utf8, -1);
			delete[] utf8;
			return result;
		}
		else
		{
			return bson_append_utf8(_document, key, -1, value, -1);
		}
	}

	bool MongoDocument::getBool(const ssi_char_t *key, bool &value)
	{
		const bson_value_t *v = BsonTools::Value(_document, key);

		if (v)
		{
			if (v->value_type == BSON_TYPE_BOOL)
			{
				value = v->value.v_bool;
				return true;
			}
		}

		return false;
	}

	bool MongoDocument::setBool(const ssi_char_t *key, bool value)
	{
		return bson_append_bool(_document, key, -1, value);
	}

	bool MongoDocument::getInt32(const ssi_char_t *key, int32_t &value)
	{
		const bson_value_t *v = BsonTools::Value(_document, key);

		if (v)
		{
			if (v->value_type == BSON_TYPE_INT32)
			{
				value = v->value.v_int32;
				return true;
			}
		}

		return false;
	}

	bool MongoDocument::setInt32(const ssi_char_t *key, int32_t value)
	{
		return bson_append_int32(_document, key, -1, value);
	}

	bool MongoDocument::getInt64(const ssi_char_t *key, int64_t &value)
	{
		const bson_value_t *v = BsonTools::Value(_document, key);

		if (v)
		{
			if (v->value_type == BSON_TYPE_INT64)
			{
				value = v->value.v_int64;
				return true;
			}
		}

		return false;
	}

	bool MongoDocument::setInt64(const ssi_char_t *key, int64_t value)
	{
		return bson_append_int64(_document, key, -1, value);
	}

	bool MongoDocument::getDouble(const ssi_char_t *key, double &value)
	{
		const bson_value_t *v = BsonTools::Value(_document, key);

		if (v)
		{
			if (v->value_type == BSON_TYPE_DOUBLE)
			{
				value = v->value.v_double;
				return true;
			}
		}

		return false;
	}

	bool MongoDocument::setDouble(const ssi_char_t *key, double value)
	{
		return bson_append_double(_document, key, -1, value);
	}

	MongoDate *MongoDocument::getDate(const ssi_char_t *key)
	{
		const bson_value_t *v = BsonTools::Value(_document, key);

		if (v)
		{
			if (v->value_type == BSON_TYPE_DATE_TIME)
			{
				MongoDate *date = new MongoDate(v->value.v_datetime);
				return date;
			}
		}

		return 0;
	}

	bool MongoDocument::setDate(const ssi_char_t *key, MongoDate *date)
	{
		return bson_append_date_time(_document, key, -1, date->getDate());
	}

	static bool VisitDocumentCallback(const bson_iter_t *iter, const char *key, const bson_t *document, void *data)
	{
		MongoDocument::document_visitor_arg_t *arg = (MongoDocument::document_visitor_arg_t*)data;
		arg->visitor->visit(MongoDocument((bson_t *)document), arg->data);
		return false;
	}

	bool MongoDocument::getArray(const ssi_char_t *key, MongoDocument::DocumentVisitor &visitor, void *data)
	{
		bson_iter_t subdoc, list;
		if (BsonTools::SubDoc(_document, key, subdoc) && BSON_ITER_HOLDS_ARRAY(&subdoc))
		{
			if (bson_iter_recurse(&subdoc, &list))
			{
				bson_visitor_t v;
				BsonTools::Init(v);
				v.visit_document = VisitDocumentCallback;
				document_visitor_arg_t arg;
				arg.data = data;
				arg.visitor = &visitor;
				return !bson_iter_visit_all(&list, &v, &arg);
			}
		}

		return false;
	}

	bool MongoDocument::setArray(const ssi_char_t *key, MongoDocuments &documents)
	{
		bson_t entries;
		bson_append_array_begin(_document, key, -1, &entries);

		ssi_char_t id[50];
		ssi_size_t i = 0;
		for (MongoDocuments::iterator it = documents.begin(); it != documents.end(); it++, i++)
		{
			ssi_sprint(id, "%u", i);
			bson_append_document(&entries, id, -1, it->get());
		}
		bson_append_array_end(_document, &entries);

		return true;
	}

	ssi_char_t *MongoDocument::UnicodeToUTF8(const ssi_char_t *str)
	{
		bson_string_t *buffer = bson_string_new("");

		ssi_size_t n = ssi_strlen(str);
		ssi_char_t *ptr = (ssi_char_t*) str;

		for (; *ptr; ptr++) {

			switch (*ptr)
			{	

			case '�':
				bson_string_append(buffer, "ï");
				break;

			case '�':
				bson_string_append(buffer, "î");
				break;

			case '�':
				bson_string_append(buffer, "ö");
				break;

			case '�':
				bson_string_append(buffer, "ô");
				break;

			case '�':
				bson_string_append(buffer, "Ü");
				break;

			case '�':
				bson_string_append(buffer, "£");
				break;

			case '�':
				bson_string_append(buffer, "é");
				break;

			case '�':
				bson_string_append(buffer, "Ä");
				break;

			case '�':
				bson_string_append(buffer, "à");
				break;

			case '�':
				bson_string_append(buffer, "ä");
				break;

			case '�':
				bson_string_append(buffer, "ê");
				break;

			case '�':
				bson_string_append(buffer, "ù");
				break;

			case '�':
				bson_string_append(buffer, "Ö");
				break;

			case '�':
				bson_string_append(buffer, "ã");
				break;

			case '�':
				bson_string_append(buffer, "û");
				break;

			case '�':
				bson_string_append(buffer, "ë");
				break;

			case '�':
				bson_string_append(buffer, "â");
				break;

			case '�':
				bson_string_append(buffer, "ü");
				break;

			case '�':
				bson_string_append(buffer, "è");
				break;

			case '�':
				bson_string_append(buffer, "É");
				break;

			case '�':
				bson_string_append(buffer, "ç");
				break;

			case '�':
				bson_string_append(buffer, "ß");
				break;

			default:
				bson_string_append_c(buffer, *ptr);
				break;
			}
		}

		ssi_char_t *result = ssi_strcpy(buffer->str);
		bson_string_free(buffer, true);
		return result;
	}

	ssi_char_t *MongoDocument::UTF8ToUnicode(const ssi_char_t *str)
	{
		bson_string_t *buffer = bson_string_new("");

		ssi_size_t n = ssi_strlen(str);
		ssi_char_t *ptr = (ssi_char_t*)str;

		for (ssi_size_t i = 0; i < n; i++, ptr++) {

			if (i < n - 1 && (*ptr == '�' || *ptr == '�'))
			{
				if (*ptr == '�')
				{
					switch (*(ptr+1))
					{
					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;
					default:
						bson_string_append_c(buffer, *ptr);
						break;
					}
				}
				else
				{
					switch (*(ptr+1))
					{

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					case '�':
						bson_string_append(buffer, "�");
						ptr++; i++;
						break;

					default:

						bson_string_append_c(buffer, *ptr);
						break;
					}
				}
			}			
			else
			{
				bson_string_append_c(buffer, *ptr);
			}
		}

		ssi_char_t *result = ssi_strcpy(buffer->str);
		bson_string_free(buffer, true);
		return result;
	}

	void MongoDocument::print(FILE *file)
	{
		if (_document)
		{
			BsonTools::Print(_document);
		}
	}

	void MongoDocuments::print(FILE *file)
	{
		MongoDocuments::iterator iter;
		for (iter = begin(); iter != end(); iter++)
		{
			iter->print(file);
		}
	}	
}