<?php

namespace App\Http\Middleware;

use Closure;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;

class EnsureUserIsUser
{
    /**
     * Handle an incoming request.
     */
    public function handle(Request $request, Closure $next)
    {
        $user = Auth::user();

        // Redirect if the user is not logged in or not a 'user' role
        if (!$user || $user->role !== 'user') {
            return redirect()->route('login')->withErrors([
                'email' => 'You must be a regular user to access this page.'
            ]);
        }

        return $next($request);
    }
}
