﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EntityFramework.Exceptions.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CleanAspire.Api.ExceptionHandlers;

public class ProblemExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public ProblemExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Instance = httpContext.Request.Path,
                Extensions = { ["errors"] = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                )
            }
            },
            UniqueConstraintException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Unique Constraint Violation",
                Detail = "A unique constraint violation occurred.",
                Instance = httpContext.Request.Path
            },
            CannotInsertNullException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Null Value Error",
                Detail = "A required field was null.",
                Instance = httpContext.Request.Path
            },
            MaxLengthExceededException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Max Length Exceeded",
                Detail = "A value exceeded the maximum allowed length.",
                Instance = httpContext.Request.Path
            },
            NumericOverflowException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Numeric Overflow",
                Detail = "A numeric value caused an overflow.",
                Instance = httpContext.Request.Path
            },
            ReferenceConstraintException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Reference Constraint Violation",
                Detail = "A foreign key reference constraint was violated.",
                Instance = httpContext.Request.Path
            },
            _ => null // Unhandled exceptions
        };

        if (problemDetails is null)
        {
            // Return true to continue processing if the exception type is not handled.
            return true;
        }

        // Write ProblemDetails to the response
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }

}
