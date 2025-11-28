<?php

namespace App\Filament\User\Widgets;

use App\Models\Project;
use Filament\Widgets\Widget;
use Filament\Forms\Concerns\InteractsWithForms;
use Filament\Forms\Contracts\HasForms;
use Filament\Forms\Components\Select;
use Illuminate\Support\Facades\Auth;

class ProjectDropDown extends Widget implements HasForms
{
    use InteractsWithForms;

    protected  string $view = 'filament.user.widgets.project-dropdown';

    public ?int $projectId = null;

    public function mount()
    {
        $this->form->fill([
            'projectId' => null,
        ]);
      
    }

   protected function getFormSchema(): array
{
    return [
        Select::make('projectId')
            ->label('Select Project')
            ->options(function () {
                $user = Auth::user();

                // If admin → show ALL projects
                if ($user->role === 'admin') {
                    return Project::pluck('project_name', 'id')->toArray();
                }

                // If normal user → show ONLY their projects
                return Project::where('user_id', $user->id)
                    ->pluck('project_name', 'id')
                    ->toArray();
            })
            ->placeholder('Choose project')
            ->reactive()
            ->afterStateUpdated(function ($state) {
                $this->projectId = $state;

                // Send event to other widgets
                $this->dispatch('projectSelected', projectId: $state);
            }),
    ];
}

}
