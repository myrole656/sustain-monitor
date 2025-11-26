<?php

namespace App\Http\Controllers\Auth;

use Filament\Facades\Filament;
use App\Http\Controllers\Controller;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;

class RoleLoginController extends Controller
{
    public function showLoginForm()
    {
        return view('auth.role-login');
    }   

    public function login(Request $request)
    {
        $request->validate([
            'email' => 'required|email',
            'password' => 'required|min:6',
            'role' => 'required|in:admin,user',
        ]);

        $credentials = $request->only('email', 'password');

        if (Auth::attempt($credentials)) {
            $user = Auth::user();

            if ($user->role !== $request->role) {
                Auth::logout();
                return back()->withErrors(['email' => 'Role mismatch.']);
            }

            $request->session()->regenerate();

            // ⭐ Set the current panel for Filament BEFORE redirect
            $panelId = $user->role === 'admin' ? 'admin' : 'user';
            Filament::setCurrentPanel(Filament::getPanel($panelId));

            // Now redirect to the correct Filament dashboard
            return redirect()->route("filament.{$panelId}.pages.dashboard");
        }

        return back()->withErrors([
            'email' => 'Invalid credentials.'
        ])->onlyInput('email');
    }



      public function logout(Request $request)
    {
        Auth::logout();
        $request->session()->invalidate();
        $request->session()->regenerateToken();

        return redirect()->route('login'); // your custom login blade
    }



}
