<?php

namespace App\Http\Controllers\Auth;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;
use App\Models\User;
use Illuminate\Support\Facades\Hash;
use Filament\Notifications\Notification;
use Illuminate\Support\Facades\Log;

class RegisterController extends Controller
{
    // -------------------------------
    // Show Registration Forms
    // -------------------------------

    public function showAdminForm()
    {
        return view('auth.register-admin');
    }

    public function showUserForm()
    {
        return view('auth.register-user');
    }

    // -------------------------------
    // Handle Registration
    // -------------------------------

    public function registerAdmin(Request $request)
    {
        $request->validate([
            'name'          => 'required|string|max:255',
            'email'         => 'required|email|unique:users,email',
            'password'      => 'required|confirmed|min:6',
            'security_code' => 'required|string',
        ]);

        $adminSecret = env('ADMIN_CODE', 'KDTMS1234');

        if ($request->security_code !== $adminSecret) {
            return back()->withErrors([
                'security_code' => 'Invalid admin code'
            ])->withInput();
        }

        // Create the admin account
        $newUser = User::create([
            'name'     => $request->name,
            'email'    => $request->email,
            'role'     => 'admin',
            'password' => Hash::make($request->password),
        ]);

        // ----------------------------
        // Send notification safely
        // ----------------------------
        try {
            // Notify all existing admins
            $admins = User::where('role', 'admin')->get();

            foreach ($admins as $admin) {
                Notification::make()
                    ->title('New Admin Registered')
                    ->body("{$newUser->name} has registered as an admin.")
                    ->icon('heroicon-o-user-plus')
                    ->iconColor('success')
                    ->sendToDatabase($admin);
            }
        } catch (\Exception $e) {
            Log::error("Admin registration notification error: " . $e->getMessage());
        }

        return redirect()->route('login')
            ->with('success', 'Admin registered successfully!');
    }


    public function registerUser(Request $request)
    {
        $request->validate([
            'name'     => 'required|string|max:255',
            'email'    => 'required|email|unique:users,email',
            'password' => 'required|confirmed|min:6',
        ]);

        // Create the user
        $newUser = User::create([
            'name'     => $request->name,
            'email'    => $request->email,
            'role'     => 'user',
            'password' => Hash::make($request->password),
        ]);

        // ----------------------------
        // Send notification safely
        // ----------------------------

        try {
            $admins = User::where('role', 'admin')->get();

            foreach ($admins as $admin) {
                Notification::make()
                    ->title('New User Registered')
                    ->body("{$newUser->name} has registered as a user.")
                    ->icon('heroicon-o-user')
                    ->iconColor('info')
                    ->sendToDatabase($admin);
            }
        } catch (\Exception $e) {
            Log::error("User registration notification error: " . $e->getMessage());
        }

        return redirect()->route('login')
            ->with('success', 'User registered successfully!');
    }
}
