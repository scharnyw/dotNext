﻿using System;
using System.Linq.Expressions;

namespace DotNext.Runtime.CompilerServices
{
    using static Metaprogramming.ExpressionBuilder;

    internal sealed class CatchStatement : GuardedStatement
    {
        internal readonly ParameterExpression ExceptionVar;
        private readonly Expression filter;

        internal CatchStatement(CatchBlock handler, LabelTarget faultLabel)
            : base(handler.Body, faultLabel)
        {
            var recovery = new RecoverFromExceptionExpression(handler.Variable is null ? Variable(typeof(Exception), "e") : handler.Variable);
            filter = handler.Filter is null ? (Expression)recovery: recovery.AndAlso(handler.Filter);
            ExceptionVar = recovery.Receiver;
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var filter = visitor.Visit(this.filter);
            if (ExpressionAttributes.Get(filter)?.ContainsAwait ?? false)
                throw new NotSupportedException(ExceptionMessages.FilterHasAwait);
            var handler = visitor.Visit(Content);
            handler = handler.AddPrologue(false, prologue).AddEpilogue(false, epilogue).AddEpilogue(false, FaultLabel.Goto());
            return IfThen(filter, handler);
        }
    }
}
