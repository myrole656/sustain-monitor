<?php

namespace App\Http\Middleware;

use Closure;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;

class EnsureUserIsAdmin
{
    /**
     * Handle an incoming request.
     */
    public function handle(Request $request, Closure $next)
    {
        $user = Auth::user();

        // If not logged in or not admin, redirect to custom login page
        if (!$user || $user->role !== 'admin') {
            return redirect()->route('login')->withErrors([
                'email' => 'You must be an admin to access this page.'
            ]);
        }

        return $next($request);
    }
}
