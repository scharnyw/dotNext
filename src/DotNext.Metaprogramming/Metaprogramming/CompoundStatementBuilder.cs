﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace DotNext.Metaprogramming
{
    using static Threading.AtomicInt64;

    /// <summary>
    /// Represents basic lexical scope support.
    /// </summary>
    public abstract class CompoundStatementBuilder: Disposable
    {
        private readonly IDictionary<string, ParameterExpression> variables;
        private readonly ICollection<Expression> statements;
        private long nameGenerator;
        private bool hasChild;

        private protected CompoundStatementBuilder(CompoundStatementBuilder parent = null)
        {
            Parent = parent;
            parent?.SendToBack();//indicates that parent scope has opened inner scope
            variables = new Dictionary<string, ParameterExpression>();
            statements = new LinkedList<Expression>();
        }

        private void SendToBack()
        {
            if (hasChild)
                throw new InvalidOperationException(ExceptionMessages.LexicalScopeIntersection);
            else
                hasChild = true;
        }

        private void BringToFront() => hasChild = false;
        
        private protected B FindScope<B>()
            where B: CompoundStatementBuilder
        {
            for (var current = this; !(current is null); current = current.Parent)
                if (current is B scope)
                    return scope;
            return null;
        }

        internal string NextName(string prefix) => Parent is null ? prefix + nameGenerator.IncrementAndGet() : Parent.NextName(prefix);

        /// <summary>
        /// Sets body of this scope as single expression.
        /// </summary>
        public virtual Expression Body
        {
            set
            {
                variables.Clear();
                statements.Clear();
                statements.Add(value);
            }
        }

        /// <summary>
        /// Represents parent scope.
        /// </summary>
        public CompoundStatementBuilder Parent{ get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected static E Build<E, B>(B builder) 
            where E: Expression
            where B: IExpressionBuilder<E>
            => builder.Build();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static E Build<E, B>(B builder, Action<B> body)
            where E: Expression
            where B: IExpressionBuilder<E>
        {
            body(builder);
            return builder.Build();
        }

        private void AddStatement<B>(Func<CompoundStatementBuilder, B> factory, Action<B> body)
            where B : IExpressionBuilder<Expression>, IDisposable
        {
            Expression statement;
            using (var builder = factory(this))
                statement = Build<Expression, B>(builder, body);
            AddStatement(statement);
        }

        //it is possible to add statement only if the current scope doesn't have opened inner scope
        private protected void ThrowIfWrongScope()
        {
            if (hasChild)
                throw new InvalidOperationException(ExceptionMessages.OuterScopeModificationDetected);
        }

        internal void AddStatement(Expression statement)
        {
            ThrowIfDisposed();
            ThrowIfWrongScope();
            statements.Add(statement);
        }

        /// <summary>
        /// Adds no-operation instruction to this scope.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Nop() => AddStatement(Expression.Empty());

        /// <summary>
        /// Adds assignment operation to this scope.
        /// </summary>
        /// <param name="variable">The variable to modify.</param>
        /// <param name="value">The value to be assigned to the variable.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Assign(ParameterExpression variable, UniversalExpression value)
            => AddStatement(variable.Assign(value));

        /// <summary>
        /// Adds assignment operation to this scope.
        /// </summary>
        /// <param name="indexer">The indexer property or array element to modify.</param>
        /// <param name="value">The value to be assigned to the member or array element.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Assign(IndexExpression indexer, UniversalExpression value)
            => AddStatement(indexer.Assign(value));

        /// <summary>
        /// Adds assignment operation to this scope.
        /// </summary>
        /// <param name="member">The field or property to modify.</param>
        /// <param name="value">The value to be assigned to the member.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Assign(MemberExpression member, UniversalExpression value)
            => AddStatement(member.Assign(value));

        /// <summary>
        /// Adds an expression that increments given variable by 1 and assigns the result back to the variable.
        /// </summary>
        /// <param name="variable">The variable to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PreIncrementAssign(ParameterExpression variable)
            => AddStatement(variable.PreIncrementAssign());

        /// <summary>
        /// Adds an expression that represents the assignment of given variable followed by a subsequent increment by 1 of the original variable.
        /// </summary>
        /// <param name="variable">The variable to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PostIncrementAssign(ParameterExpression variable)
            => AddStatement(variable.PostIncrementAssign());

        /// <summary>
        /// Adds an expression that decrements given variable by 1 and assigns the result back to the variable.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PreDecrementAssign(ParameterExpression variable)
            => AddStatement(variable.PreDecrementAssign());

        /// <summary>
        /// Adds an expression that represents the assignment of given variable followed by a subsequent decrement by 1 of the original variable.
        /// </summary>
        /// <param name="variable">The variable to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PostDecrementAssign(ParameterExpression variable)
            => AddStatement(variable.PostDecrementAssign());

        /// <summary>
        /// Adds an expression that increments given field or property by 1 and assigns the result back to the member.
        /// </summary>
        /// <param name="member">The member to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PreIncrementAssign(MemberExpression member)
            => AddStatement(member.PreIncrementAssign());

        /// <summary>
        /// Adds an expression that represents the assignment of given field or property followed by a subsequent increment by 1 of the original member.
        /// </summary>
        /// <param name="member">The member to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PostIncrementAssign(MemberExpression member)
            => AddStatement(member.PostIncrementAssign());

        /// <summary>
        /// Adds an expression that decrements given field or property by 1 and assigns the result back to the member.
        /// </summary>
        /// <param name="member">The member to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PreDecrementAssign(MemberExpression member)
            => AddStatement(member.PreDecrementAssign());

        /// <summary>
        /// Adds an expression that represents the assignment of given field or property followed by a subsequent decrement by 1 of the original member.
        /// </summary>
        /// <param name="member">The member to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PostDecrementAssign(MemberExpression member)
            => AddStatement(member.PostDecrementAssign());

        /// <summary>
        /// Adds an expression that increments given field or property by 1 and assigns the result back to the member.
        /// </summary>
        /// <param name="member">The member to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PreIncrementAssign(IndexExpression member)
            => AddStatement(member.PreIncrementAssign());

        /// <summary>
        /// Adds an expression that represents the assignment of given field or property followed by a subsequent increment by 1 of the original member.
        /// </summary>
        /// <param name="member">The member to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PostIncrementAssign(IndexExpression member)
            => AddStatement(member.PostIncrementAssign());

        /// <summary>
        /// Adds an expression that decrements given field or property by 1 and assigns the result back to the member.
        /// </summary>
        /// <param name="member">The member to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PreDecrementAssign(IndexExpression member)
            => AddStatement(member.PreDecrementAssign());

        /// <summary>
        /// Adds an expression that represents the assignment of given field or property followed by a subsequent decrement by 1 of the original member.
        /// </summary>
        /// <param name="member">The member to be modified.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void PostDecrementAssign(IndexExpression member)
            => AddStatement(member.PostDecrementAssign());

        /// <summary>
        /// Adds local variable assignment operation this scope.
        /// </summary>
        /// <param name="variableName">The name of the declared local variable.</param>
        /// <param name="value">The value to be assigned to the local variable.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Assign(string variableName, UniversalExpression value)
            => Assign(this[variableName], value);

        /// <summary>
        /// Adds instance property assignment.
        /// </summary>
        /// <param name="instance"><see langword="this"/> argument.</param>
        /// <param name="instanceProperty">Instance property to be assigned.</param>
        /// <param name="value">A new value of the property.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Assign(UniversalExpression instance, PropertyInfo instanceProperty, UniversalExpression value)
            => AddStatement(Expression.Assign(Expression.Property(instance, instanceProperty), value));

        /// <summary>
        /// Adds static property assignment.
        /// </summary>
        /// <param name="staticProperty">Static property to be assigned.</param>
        /// <param name="value">A new value of the property.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Assign(PropertyInfo staticProperty, UniversalExpression value)
            => AddStatement(Expression.Assign(Expression.Property(null, staticProperty), value));

        /// <summary>
        /// Adds instance field assignment.
        /// </summary>
        /// <param name="instance"><see langword="this"/> argument.</param>
        /// <param name="instanceField">Instance field to be assigned.</param>
        /// <param name="value">A new value of the field.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Assign(UniversalExpression instance, FieldInfo instanceField, UniversalExpression value)
            => AddStatement(Expression.Assign(Expression.Field(instance, instanceField), value));

        /// <summary>
        /// Adds static field assignment.
        /// </summary>
        /// <param name="staticField">Static field to be assigned.</param>
        /// <param name="value">A new value of the field.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Assign(FieldInfo staticField, UniversalExpression value)
            => AddStatement(Expression.Assign(Expression.Field(null, staticField), value));

        /// <summary>
        /// Adds invocation statement.
        /// </summary>
        /// <param name="delegate">The expression providing delegate to be invoked.</param>
        /// <param name="arguments">Delegate invocation arguments.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Invoke(UniversalExpression @delegate, IEnumerable<Expression> arguments)
            => AddStatement(Expression.Invoke(@delegate, arguments));

        /// <summary>
        /// Adds invocation statement.
        /// </summary>
        /// <param name="delegate">The expression providing delegate to be invoked.</param>
        /// <param name="arguments">Delegate invocation arguments.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Invoke(UniversalExpression @delegate, params UniversalExpression[] arguments)
            => AddStatement(@delegate.Invoke(arguments));

        /// <summary>
        /// Adds instance method call statement.
        /// </summary>
        /// <param name="instance"><see langword="this"/> argument.</param>
        /// <param name="method">The method to be called.</param>
        /// <param name="arguments">Method call arguments.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Call(UniversalExpression instance, MethodInfo method, IEnumerable<Expression> arguments)
            => AddStatement(Expression.Call(instance, method, arguments));

        /// <summary>
        /// Adds instance method call statement.
        /// </summary>
        /// <param name="instance"><see langword="this"/> argument.</param>
        /// <param name="method">The method to be called.</param>
        /// <param name="arguments">Method call arguments.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Call(UniversalExpression instance, MethodInfo method, params UniversalExpression[] arguments)
            => Call(instance, method, UniversalExpression.AsExpressions((IEnumerable<UniversalExpression>)arguments));

        /// <summary>
        /// Adds instance method call statement.
        /// </summary>
        /// <param name="instance"><see langword="this"/> argument.</param>
        /// <param name="methodName">The method to be called.</param>
        /// <param name="arguments">Method call arguments.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Call(UniversalExpression instance, string methodName, params UniversalExpression[] arguments)
            => AddStatement(instance.Call(methodName, arguments));

        /// <summary>
        /// Adds static method call statement.,
        /// </summary>
        /// <param name="method">The method to be called.</param>
        /// <param name="arguments">Method call arguments.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Call(MethodInfo method, IEnumerable<Expression> arguments)
            => AddStatement(Expression.Call(null, method, arguments));

        /// <summary>
        /// Adds static method call statement.
        /// </summary>
        /// <param name="method">The method to be called.</param>
        /// <param name="arguments">Method call arguments.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Call(MethodInfo method, params UniversalExpression[] arguments)
            => Call(method, UniversalExpression.AsExpressions((IEnumerable<UniversalExpression>)arguments));

        /// <summary>
        /// Declares label of the specified type.
        /// </summary>
        /// <param name="type">The type of landing site.</param>
        /// <param name="name">The optional name of the label.</param>
        /// <returns>Declared label.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public LabelTarget Label(Type type, string name = null)
        {
            var target = Expression.Label(type, name);
            Label(target);
            return target;
        }

        /// <summary>
        /// Declares label of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of landing site.</typeparam>
        /// <param name="name">The optional name of the label.</param>
        /// <returns>Declared label.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public LabelTarget Label<T>(string name = null) => Label(typeof(T), name);

        /// <summary>
        /// Declares label in the current scope.
        /// </summary>
        /// <returns>Declared label.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public LabelTarget Label() => Label(typeof(void));

        /// <summary>
        /// Adds label landing site to this scope.
        /// </summary>
        /// <param name="target">The label target.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Label(LabelTarget target) => AddStatement(Expression.Label(target));

        /// <summary>
        /// Adds unconditional control transfer statement to this scope.
        /// </summary>
        /// <param name="target">The label reference.</param>
        /// <param name="value">The value to be associated with the control transfer.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Goto(LabelTarget target, UniversalExpression value)
            => AddStatement(Expression.Goto(target, value));

        /// <summary>
        /// Adds unconditional control transfer statement to this scope.
        /// </summary>
        /// <param name="target">The label reference.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Goto(LabelTarget target) => Goto(target, default);

        private bool HasVariable(string name) => variables.ContainsKey(name) || Parent != null && Parent.HasVariable(name);
        
        /// <summary>
        /// Gets declared local variable in the current or parent scope.
        /// </summary>
        /// <param name="localVariableName">The name of the local variable.</param>
        /// <returns>Declared local variable; or <see langword="null"/>, if there is no declared local variable with the given name.</returns>
        public ParameterExpression this[string localVariableName]
            => variables.TryGetValue(localVariableName, out var variable) ? variable : Parent?[localVariableName];

        private protected void DeclareVariable(ParameterExpression variable)
            => variables.Add(variable.Name, variable);

        /// <summary>
        /// Declares local variable in the current lexical scope.
        /// </summary>
        /// <typeparam name="T">The type of local variable.</typeparam>
        /// <param name="name">The name of local variable.</param>
        /// <returns>The expression representing local variable.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public ParameterExpression DeclareVariable<T>(string name)
            => DeclareVariable(typeof(T), name);

        /// <summary>
        /// Declares local variable in the current lexical scope. 
        /// </summary>
        /// <param name="variableType">The type of local variable.</param>
        /// <param name="name">The name of local variable.</param>
        /// <returns>The expression representing local variable.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public ParameterExpression DeclareVariable(Type variableType, string name)
        {
            ThrowIfDisposed();
            ThrowIfWrongScope();
            var variable = Expression.Variable(variableType, name);
            DeclareVariable(variable);
            return variable;
        }

        /// <summary>
        /// Declares initialized local variable of automatically
        /// inferred type.
        /// </summary>
        /// <remarks>
        /// The equivalent code is <c>var i = expr;</c>
        /// </remarks>
        /// <param name="name">The name of the variable.</param>
        /// <param name="init">Initialization expression.</param>
        /// <returns>The expression representing local variable.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public ParameterExpression DeclareVariable(string name, UniversalExpression init)
        {
            var variable = DeclareVariable(init.Type, name);
            Assign(variable, init);
            return variable;
        }

        /// <summary>
        /// Adds await operator.
        /// </summary>
        /// <param name="asyncResult">The expression representing asynchronous computing process.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Await(UniversalExpression asyncResult) => AddStatement(asyncResult.Await());

        /// <summary>
        /// Adds if-then-else statement to this scope.
        /// </summary>
        /// <param name="test">Test expression.</param>
        /// <returns>Conditional statement builder.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public ConditionalBuilder If(UniversalExpression test)
            => new ConditionalBuilder(test, this, true);

        /// <summary>
        /// Adds if-then statement to this scope.
        /// </summary>
        /// <param name="test">Test expression.</param>
        /// <param name="ifTrue">Positive branch builder.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void IfThen(UniversalExpression test, Action<CompoundStatementBuilder> ifTrue)
            => If(test).Then(ifTrue).End();

        /// <summary>
        /// Adds if-then-else statement to this scope.
        /// </summary>
        /// <param name="test">Test expression.</param>
        /// <param name="ifTrue">Positive branch builder.</param>
        /// <param name="ifFalse">Negative branch builder.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void IfThenElse(UniversalExpression test, Action<CompoundStatementBuilder> ifTrue, Action<CompoundStatementBuilder> ifFalse)
            => If(test).Then(ifTrue).Else(ifFalse).End();

        /// <summary>
        /// Adds <see langword="while"/> loop statement.
        /// </summary>
        /// <param name="test">Loop continuation condition.</param>
        /// <param name="loop">Loop body.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void While(UniversalExpression test, Action<WhileLoopBuider> loop)
            => AddStatement(parent => new WhileLoopBuider(test, parent, true), loop);

        /// <summary>
        /// Adds <code>do{ } while(condition);</code> loop statement.
        /// </summary>
        /// <param name="test">Loop continuation condition.</param>
        /// <param name="loop">Loop body.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void DoWhile(UniversalExpression test, Action<WhileLoopBuider> loop)
            => AddStatement(parent => new WhileLoopBuider(test, parent, false), loop);

        /// <summary>
        /// Adds <see langword="foreach"/> loop statement.
        /// </summary>
        /// <param name="collection">The expression providing enumerable collection.</param>
        /// <param name="loop">Loop body.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        /// <seealso cref="ForEachLoopBuilder"/>
        public void ForEach(UniversalExpression collection, Action<ForEachLoopBuilder> loop)
            => AddStatement(parent => new ForEachLoopBuilder(collection, parent), loop);

        /// <summary>
        /// Adds <see langword="for"/> loop statement.
        /// </summary>
        /// <remarks>
        /// This builder constructs the statement equivalent to <code>for(var i = initializer; condition; iter){ body; }</code>
        /// </remarks>
        /// <param name="initializer">Loop variable initialization expression.</param>
        /// <param name="condition">Loop continuation condition.</param>
        /// <param name="loop">Loop body.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        /// <seealso cref="ForLoopBuilder"/>
        public void For(UniversalExpression initializer, Func<UniversalExpression, Expression> condition, Action<ForLoopBuilder> loop)
            => AddStatement(parent => new ForLoopBuilder(initializer, condition, parent), loop);

        /// <summary>
        /// Adds generic loop statement.
        /// </summary>
        /// <param name="loop">Loop body.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        /// <seealso cref="LoopBuilder"/>
        public void Loop(Action<LoopBuilder> loop) => AddStatement(parent => new LoopBuilder(parent), loop);

        /// <summary>
        /// Constructs lamdba function capturing the current lexical scope.
        /// </summary>
        /// <typeparam name="D">The delegate describing signature of lambda function.</typeparam>
        /// <param name="lambda">Lambda function builder.</param>
        /// <returns>Constructed lambda expression.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public LambdaExpression Lambda<D>(Action<LambdaBuilder<D>> lambda)
            where D : Delegate
        {
            ThrowIfDisposed();
            ThrowIfWrongScope();
            using (var builder = new LambdaBuilder<D>(this))
                return Build<LambdaExpression, LambdaBuilder<D>>(builder, lambda);
        }

        /// <summary>
        /// Constructs async lambda function capturing the current lexical scope.
        /// </summary>
        /// <typeparam name="D">The delegate describing signature of lambda function.</typeparam>
        /// <param name="lambda">Lambda function builder.</param>
        /// <returns>Constructed lambda expression.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        /// <seealso cref="AwaitExpression"/>
        /// <seealso cref="AsyncResultExpression"/>
        /// <seealso cref="AsyncLambdaBuilder{D}"/>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/#BKMK_HowtoWriteanAsyncMethod">Async methods</seealso>
        public LambdaExpression AsyncLambda<D>(Action<AsyncLambdaBuilder<D>> lambda)
            where D : Delegate
        {
            ThrowIfDisposed();
            ThrowIfWrongScope();
            using (var builder = new AsyncLambdaBuilder<D>(this))
                return Build<LambdaExpression, AsyncLambdaBuilder<D>>(builder, lambda);
        }

        /// <summary>
        /// Adds structured exception handling statement.
        /// </summary>
        /// <param name="body"><see langword="try"/> block.</param>
        /// <returns>Structured exception handling builder.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public TryBuilder Try(UniversalExpression body) => new TryBuilder(body, this, true);

        /// <summary>
        /// Adds structured exception handling statement.
        /// </summary>
        /// <param name="scope"><see langword="try"/> block builder.</param>
        /// <returns>Structured exception handling builder.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public TryBuilder Try(Action<ScopeBuilder> scope)
        {
            Expression tryBlock;
            using (var tryScope = new ScopeBuilder(this))
                tryBlock = tryScope.Build(scope);
            return Try(tryBlock);
        }

        /// <summary>
        /// Adds <see langword="throw"/> statement to the compound statement.
        /// </summary>
        /// <param name="exception">The exception to be thrown.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Throw(UniversalExpression exception) => AddStatement(Expression.Throw(exception));

        /// <summary>
        /// Adds <see langword="throw"/> statement to the compound statement.
        /// </summary>
        /// <typeparam name="E">The exception to be thrown.</typeparam>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Throw<E>()
            where E : Exception, new()
            => Throw(Expression.New(typeof(E).GetConstructor(Array.Empty<Type>())));

        /// <summary>
        /// Constructs nested lexical scope.
        /// </summary>
        /// <param name="scope">The code block builder.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Scope(Action<ScopeBuilder> scope)
        {
            Expression statement;
            using (var scopeBuilder = new ScopeBuilder(this))
                statement = scopeBuilder.Build(scope);
            AddStatement(statement);
        }

        /// <summary>
        /// Constructs compound statement hat repeatedly refer to a single object or 
        /// structure so that the statements can use a simplified syntax when accessing members 
        /// of the object or structure.
        /// </summary>
        /// <param name="expression">The implicitly referenced object.</param>
        /// <param name="scope">The statement body.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        /// <seealso cref="WithBlockBuilder.ScopeVar"/>
        public void With(UniversalExpression expression, Action<WithBlockBuilder> scope)
            => AddStatement(parent => new WithBlockBuilder(expression, parent), scope);

        /// <summary>
        /// Adds <see langword="using"/> statement.
        /// </summary>
        /// <param name="expression">The expression representing disposable resource.</param>
        /// <param name="scope">The body of the statement.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Using(UniversalExpression expression, Action<UsingBlockBuilder> scope)
            => AddStatement(parent => new UsingBlockBuilder(expression, parent), scope);

        /// <summary>
        /// Adds <see langword="lock"/> statement.
        /// </summary>
        /// <param name="syncRoot">The object to be locked during execution of the compound statement.</param>
        /// <param name="scope">Synchronized scope of code.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public void Lock(UniversalExpression syncRoot, Action<LockBuilder> scope)
            => AddStatement(parent => new LockBuilder(syncRoot, parent), scope);

        /// <summary>
        /// Adds selection expression.
        /// </summary>
        /// <param name="switchValue">The value to be handled by the selection expression.</param>
        /// <returns>Selection expression builder.</returns>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public SwitchBuilder Switch(UniversalExpression switchValue) => new SwitchBuilder(switchValue, this, true);

        /// <summary>
        /// Adds <see langword="return"/> instruction to return from
        /// underlying lambda function having <see langword="void"/>
        /// return type.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public abstract void Return();

        /// <summary>
        /// Adds <see langword="return"/> instruction to return from
        /// underlying lambda function having non-<see langword="void"/>
        /// return type.
        /// </summary>
        /// <param name="result">The value to be returned from the lambda function.</param>
        /// <exception cref="ObjectDisposedException">This lexical scope is closed.</exception>
        /// <exception cref="InvalidOperationException">Attempts to call this method for the outer scope from the inner scope.</exception>
        public abstract void Return(UniversalExpression result);

        internal virtual Expression Build()
        {
            ThrowIfDisposed();
            switch(statements.Count)
            {
                case 0:
                    return Expression.Empty();
                case 1:
                    if(variables.Count == 0 && statements.Count == 1)
                        return statements.First();
                    else
                        goto default;
                default:
                    return Expression.Block(variables.Values, statements);
            }
        }

        /// <summary>
        /// Releases all resources associated with this builder.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>, if this method is called from <see cref="Disposable.Dispose()"/>; <see langword="false"/> if called from finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                variables.Clear();
                statements.Clear();
                Parent?.BringToFront();
            }
        }
    }
}
