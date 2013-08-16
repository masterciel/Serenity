﻿using Serenity.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using EntityType = System.String;

namespace Serenity.Services
{
    public class CreateRequestHandler<TRow, TCreateRequest, TCreateResponse>
        where TRow : Row, IIdRow, new()
        where TCreateRequest: SaveRequest<TRow>
        where TCreateResponse : CreateResponse, new()
    {
        protected TRow Row;
        protected IUnitOfWork UnitOfWork;
        protected TCreateRequest Request;
        protected TCreateResponse Response;

        private static bool loggingInitialized;
        protected static CaptureLogHandler<TRow> captureLogHandler;

        protected IDbConnection Connection 
        { 
            get { return UnitOfWork.Connection; } 
        }

        protected virtual void GetEditableFields(HashSet<Field> editable)
        {
            foreach (var field in Row.GetFields())
                if ((field.Flags & FieldFlags.Insertable) == FieldFlags.Insertable)
                    editable.Add(field);
        }

        protected virtual void GetRequiredFields(HashSet<Field> required, HashSet<Field> editable)
        {
            foreach (var field in Row.GetFields())
                if (editable.Contains(field) &&
                    (field.Flags & FieldFlags.NotNull) == FieldFlags.NotNull &
                    (field.Flags & FieldFlags.TrimToEmpty) != FieldFlags.TrimToEmpty)
                    required.Add(field);
        }

        protected virtual void SetInternalFields()
        {
            var loggingRow = Row as ILoggingRow;
            if (loggingRow != null)
            {
                loggingRow.InsertDateField[Row] = DateTime.UtcNow;
                loggingRow.InsertUserIdField[Row] = SecurityHelper.CurrentUserId;
            }

            var isActiveRow = Row as IIsActiveRow;
            if (isActiveRow != null)
                isActiveRow.IsActiveField[Row] = 1;

            foreach (var field in Row.GetFields())
                if (!Row.IsAssigned(field) &&
                    (field is StringField &&
                    (field.Flags & FieldFlags.Insertable) == FieldFlags.Insertable &
                    (field.Flags & FieldFlags.NotNull) == FieldFlags.NotNull &
                    (field.Flags & FieldFlags.TrimToEmpty) == FieldFlags.TrimToEmpty))
                {
                    ((StringField)field)[Row] = "";
                }
        }

        protected virtual void ClearNonTableAssignments()
        {
            foreach (var field in Row.GetFields())
                if (Row.IsAssigned(field) &&
                    (field.Flags & FieldFlags.Foreign) == FieldFlags.Foreign ||
                    (field.Flags & FieldFlags.Calculated) == FieldFlags.Calculated ||
                    (field.Flags & FieldFlags.Reflective) == FieldFlags.Reflective ||
                    (field.Flags & FieldFlags.ClientSide) == FieldFlags.ClientSide)
                {
                    Row.ClearAssignment(field);
                }
        }

        protected virtual void OnBeforeInsert()
        {
        }

        protected virtual void OnAfterInsert()
        {
        }

        protected virtual void OnReturn()
        {
        }

        protected AuditInsertRequest GetAuditRequest(HashSet<Field> auditFields)
        {
            EntityType entityType = Row.Table;

            Field[] array = new Field[auditFields.Count];
            auditFields.CopyTo(array);
            var auditRequest = new AuditInsertRequest(entityType, Row, array);

            var parentIdRow = Row as IParentIdRow;
            if (parentIdRow != null)
            {
                var parentIdField = (Field)parentIdRow.ParentIdField;
                if (!parentIdField.ForeignTable.IsTrimmedEmpty())
                {
                    auditRequest.ParentTypeId = parentIdField.ForeignTable;
                    auditRequest.ParentId = parentIdRow.ParentIdField[Row];
                }
            }

            return auditRequest;
        }

        protected virtual void HandleNonEditable(Field field)
        {
            if (!field.IsNull(Row) &&
                (field.Flags & FieldFlags.Reflective) != FieldFlags.Reflective)
            {
                bool isNonTableField = ((field.Flags & FieldFlags.Foreign) == FieldFlags.Foreign) ||
                      ((field.Flags & FieldFlags.Calculated) == FieldFlags.Calculated) ||
                      ((field.Flags & FieldFlags.ClientSide) == FieldFlags.ClientSide);

                if (!isNonTableField)
                    throw DataValidation.ReadOnlyError(Row, field);

                field.AsObject(Row, null);
                Row.ClearAssignment(field);
            }

            if (Row.IsAssigned(field))
                Row.ClearAssignment(field);
        }

        protected virtual void ValidateEditableFields(HashSet<Field> editable)
        {
            foreach (Field field in Row.GetFields())
            {
                var stringField = field as StringField;
                if (stringField != null && 
                    Row.IsAssigned(field) &&
                    (field.Flags & FieldFlags.Trim) == FieldFlags.Trim)
                {
                    string value = stringField[Row];
                    
                    if ((field.Flags & FieldFlags.TrimToEmpty) == FieldFlags.TrimToEmpty)
                        value = value.TrimToEmpty();
                    else // TrimToNull
                        value = value.TrimToNull();

                    stringField[Row] = value;
                }

                if (!editable.Contains(field))
                    HandleNonEditable(field);
            }
        }

        protected virtual void ValidateRequired(HashSet<Field> editableFields)
        {
            var requiredFields = new HashSet<Field>();
            GetRequiredFields(required: requiredFields, editable: editableFields);
            Row.ValidateRequired(requiredFields);
        }

        protected virtual HashSet<Field> ValidateEditable()
        {
            var editableFields = new HashSet<Field>();
            GetEditableFields(editableFields);
            ValidateEditableFields(editableFields);
            return editableFields;
        }

        protected virtual void ValidateRequest()
        {
            var editableFields = ValidateEditable();
            ValidateRequired(editableFields);
            ValidateParent();
        }

        protected virtual void ValidateParent()
        {
            var parentIdRow = Row as IParentIdRow;
            if (parentIdRow == null)
                return;

            var parentId = parentIdRow.ParentIdField[Row];
            if (parentId == null)
                return;
                    
            var parentIdField = (Field)parentIdRow.ParentIdField;
            if (parentIdField.ForeignTable.IsEmptyOrNull())
                return;

            var foreignRow = RowRegistry.GetSchemaRow(RowRegistry.GetSchemaName(Row), parentIdField.ForeignTable);
            if (foreignRow == null)
                return;

            var idForeign = (IIdRow)foreignRow;
            if (idForeign == null)
                return;

            var isActiveForeign = (IIsActiveRow)foreignRow;
            if (isActiveForeign == null)
                return;

            ServiceHelper.CheckParentNotDeleted(Connection, foreignRow.Table, query => query
                .Where(q =>
                    new Filter((Field)idForeign.IdField) == q.Param(parentId) &
                    new Filter(isActiveForeign.IsActiveField) < q.Param(0)));
        }

        protected virtual void InvalidateCacheOnCommit()
        {
            var attr = typeof(TRow).GetCustomAttribute<TwoLevelCachedAttribute>(false);
            if (attr != null)
            {
                BatchGenerationUpdater.OnCommit(this.UnitOfWork, Row.GetFields().GenerationKey);
                foreach (var key in attr.GenerationKeys)
                    BatchGenerationUpdater.OnCommit(this.UnitOfWork, key);
            }
        }

        protected virtual void ExecuteInsert()
        {
            Response.EntityId = new SqlInsert(Row)
                .ExecuteAndGetID(Connection).Value;

            Row.IdField[Row] = Response.EntityId;

            InvalidateCacheOnCommit();
        }

        protected virtual void DoGenericAudit()
        {
            var auditFields = new HashSet<Field>();
            GetEditableFields(auditFields);

            var auditRequest = GetAuditRequest(auditFields);
            if (auditRequest != null)
                AuditLogService.AuditInsert(Connection, RowRegistry.GetSchemaName(Row), auditRequest);
        }

        protected virtual void DoCaptureLog()
        {
            captureLogHandler.Log(this.UnitOfWork, this.Row, SecurityHelper.CurrentUserId, isDelete: false);
        }

        protected virtual void DoAudit()
        {
            if (!loggingInitialized)
            {
                var logTableAttr = Row.GetType().GetCustomAttribute<CaptureLogAttribute>();
                if (logTableAttr != null)
                    captureLogHandler = new CaptureLogHandler<TRow>();

                loggingInitialized = true;
            }

            if (captureLogHandler != null)
            {
                DoCaptureLog();
            }
            else
                DoGenericAudit();
        }

        protected virtual void ValidatePermissions()
        {
            var modifyAttr = typeof(TRow).GetCustomAttribute<ModifyPermissionAttribute>(false);
            if (modifyAttr != null)
            {
                if (modifyAttr.ModifyPermission.IsEmptyOrNull())
                    SecurityHelper.EnsureLoggedIn(RightErrorHandling.ThrowException);
                else
                    SecurityHelper.EnsurePermission(modifyAttr.ModifyPermission, RightErrorHandling.ThrowException);
            }
        }

        public TCreateResponse Process(IUnitOfWork unitOfWork, TCreateRequest request)
        {
            if (unitOfWork == null)
                throw new ArgumentNullException("unitOfWork");

            ValidatePermissions();

            UnitOfWork = unitOfWork;

            Request = request;
            Response = new TCreateResponse();

            Row = request.Entity;
            if (Row == null)
                throw new ArgumentNullException("Entity");

            ValidateRequest();
            SetInternalFields();
            OnBeforeInsert();
            ClearNonTableAssignments();
            ExecuteInsert();
            OnAfterInsert();
            DoAudit();
            OnReturn();

            return Response;
        }
    }

    public class CreateRequestHandler<TRow> : CreateRequestHandler<TRow, SaveRequest<TRow>, CreateResponse>
        where TRow : Row, IIdRow, new()
    {
    }
}